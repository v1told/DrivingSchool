using DrivingSchool.Core.Common;
using DrivingSchool.Core.Data;
using DrivingSchool.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DrivingSchool.Core.Scheduling;

/// <summary>Результат автоматичного формування розкладу.</summary>
public class GenerationResult
{
    public int TheoryCreated { get; set; }
    public int DrivingSlotsCreated { get; set; }
    public List<string> Warnings { get; } = new();

    public string Summary =>
        $"Створено теоретичних занять: {TheoryCreated}; вільних слотів водіння: {DrivingSlotsCreated}." +
        (Warnings.Count > 0 ? "\nЗауваження:\n" + string.Join("\n", Warnings.Select(w => "• " + w)) : string.Empty);
}

/// <summary>
/// Автоматичне формування тижневого розкладу жадібним алгоритмом
/// з перевіркою обмежень і балансуванням навантаження викладачів.
/// Теорія розподіляється між групами, а практичні слоти створюються
/// в обсязі, що відповідає реальній потребі учнів.
/// </summary>
public class ScheduleGenerator
{
    private readonly AppDbContext _db;
    private readonly ScheduleGenerationOptions _options;
    private readonly ScheduleConflictChecker _checker;

    private List<Lesson> _weekLessons = new();
    private readonly Dictionary<int, int> _load = new();

    public ScheduleGenerator(AppDbContext db, ScheduleGenerationOptions? options = null,
        ScheduleConflictChecker? checker = null)
    {
        _db = db;
        _options = options ?? new ScheduleGenerationOptions();
        _checker = checker ?? ScheduleConflictChecker.CreateDefault();
    }

    public GenerationResult GenerateWeek(DateTime anyDayOfWeek)
    {
        var weekStart = DateHelper.StartOfWeek(anyDayOfWeek);
        var weekEnd = weekStart.AddDays(7);
        var result = new GenerationResult();

        _weekLessons = _db.Lessons
            .Where(l => l.Start >= weekStart && l.Start < weekEnd && l.Status != LessonStatus.Cancelled)
            .ToList();
        _load.Clear();
        foreach (var lesson in _weekLessons)
        {
            _load[lesson.InstructorId] = Load(lesson.InstructorId) + 1;
        }

        GenerateTheory(weekStart, weekEnd, result);
        GenerateDriving(weekStart, weekEnd, result);

        _db.SaveChanges();
        return result;
    }

    private int Load(int instructorId) => _load.TryGetValue(instructorId, out var n) ? n : 0;

    private void GenerateTheory(DateTime weekStart, DateTime weekEnd, GenerationResult result)
    {
        var teachers = _db.Instructors.Where(i => i.TeachesTheory).ToList();
        if (teachers.Count == 0)
        {
            result.Warnings.Add("Немає викладачів теоретичного курсу.");
            return;
        }

        var groups = _db.Groups.ToList()
            .Where(g => g.StartDate.Date < weekEnd && g.EndDate.Date >= weekStart)
            .ToList();

        foreach (var group in groups)
        {
            int already = _weekLessons.OfType<TheoryLesson>().Count(l => l.GroupId == group.Id);
            int needed = _options.TheoryLessonsPerWeek - already;

            foreach (var day in _options.TheoryDays)
            {
                if (needed <= 0) break;
                var date = weekStart.AddDays(((int)day + 6) % 7);
                if (!group.IsRunningOn(date)) continue;
                if (_options.NotBefore.HasValue && date.Add(_options.TheoryStartTimes.Max()) < _options.NotBefore.Value) continue;
                // Не більше одного заняття групи на день.
                if (_weekLessons.OfType<TheoryLesson>().Any(l => l.GroupId == group.Id && l.Start.Date == date))
                    continue;

                if (TryPlaceTheory(group, date, teachers))
                {
                    result.TheoryCreated++;
                    needed--;
                }
            }

            if (needed > 0)
            {
                result.Warnings.Add($"Групі {group.Name} не вистачило {needed} теоретичних занять: немає вільного часу.");
            }
        }
    }

    private bool TryPlaceTheory(StudyGroup group, DateTime date, List<Instructor> teachers)
    {
        foreach (var time in _options.TheoryStartTimes)
        {
            if (_options.NotBefore.HasValue && date.Add(time) < _options.NotBefore.Value) continue;
            // Спочатку пробуємо найменш завантажених викладачів.
            foreach (var teacher in teachers.OrderBy(t => Load(t.Id)))
            {
                var lesson = new TheoryLesson
                {
                    GroupId = group.Id,
                    InstructorId = teacher.Id,
                    Classroom = group.Classroom,
                    Start = date.Add(time),
                    DurationMinutes = TheoryLesson.DefaultDuration,
                    Topic = "Теоретичний курс"
                };
                var ctx = new ScheduleContext(lesson, teacher, _weekLessons);
                if (_checker.IsValid(ctx))
                {
                    Accept(lesson);
                    return true;
                }
            }
        }
        return false;
    }

    private void GenerateDriving(DateTime weekStart, DateTime weekEnd, GenerationResult result)
    {
        var instructors = _db.Instructors
            .Include(i => i.Vehicle)
            .Where(i => i.TeachesDriving && i.VehicleId != null)
            .ToList()
            .Where(i => i.Vehicle!.IsAvailable)
            .ToList();

        if (instructors.Count == 0)
        {
            result.Warnings.Add("Немає інструкторів із закріпленим справним автомобілем.");
            return;
        }

        // Попит рахується окремо для кожної пари «категорія + коробка передач».
        var demand = CalculateDemand(weekStart, weekEnd);

        foreach (var pool in instructors.GroupBy(i => (Category: i.Vehicle!.Category, Transmission: i.Vehicle!.Transmission)))
        {
            demand.TryGetValue(pool.Key, out int needed);
            int existing = _weekLessons.OfType<DrivingLesson>()
                .Count(l => pool.Any(i => i.Id == l.InstructorId));
            needed -= existing;
            if (needed <= 0) continue;

            // Черга можливих слотів для кожного інструктора. Слоти впорядковано
            // спочатку за часом, потім за днем, щоб рівномірно заповнити весь тиждень.
            var dates = _options.DrivingDays.Select(d => weekStart.AddDays(((int)d + 6) % 7)).ToList();
            var queues = pool.ToDictionary(
                i => i,
                i => new Queue<DateTime>(
                    _options.DrivingStartTimes
                        .SelectMany(time => dates.Select(date => date.Add(time)))
                        .Where(start => _options.NotBefore == null || start >= _options.NotBefore.Value)));

            while (needed > 0 && queues.Values.Any(q => q.Count > 0))
            {
                // Балансування: беремо інструктора з найменшим навантаженням.
                var instructor = queues.Where(kv => kv.Value.Count > 0)
                    .Select(kv => kv.Key)
                    .OrderBy(i => Load(i.Id))
                    .First();
                var start = queues[instructor].Dequeue();

                int perDay = _weekLessons.OfType<DrivingLesson>()
                    .Count(l => l.InstructorId == instructor.Id && l.Start.Date == start.Date);
                if (perDay >= _options.MaxDrivingSlotsPerInstructorPerDay) continue;

                var slot = new DrivingLesson
                {
                    InstructorId = instructor.Id,
                    VehicleId = instructor.VehicleId!.Value,
                    Start = start,
                    DurationMinutes = DrivingLesson.DefaultDuration,
                    Topic = "Практичне водіння"
                };
                var ctx = new ScheduleContext(slot, instructor, _weekLessons) { Vehicle = instructor.Vehicle };
                if (_checker.IsValid(ctx))
                {
                    Accept(slot);
                    result.DrivingSlotsCreated++;
                    needed--;
                }
            }

            if (needed > 0)
            {
                result.Warnings.Add(
                    $"Для категорії {pool.Key.Category} ({(pool.Key.Transmission == Transmission.Manual ? "механіка" : "автомат")}) " +
                    $"не вистачило {needed} слотів водіння.");
            }
        }
    }

    /// <summary>
    /// Тижнева потреба в практичних заняттях. Для кожного активного учня
    /// залишок годин рахується як «програма − відпрацьовано − заплановано»
    /// (DrivingHoursCalculator); до нього додаються записи учня на цей тиждень,
    /// бо зайняті ними слоти теж входять у наявні слоти тижня.
    /// </summary>
    private Dictionary<(LicenseCategory, Transmission), int> CalculateDemand(DateTime weekStart, DateTime weekEnd)
    {
        var students = _db.Students.Where(s => s.IsActive).ToList();
        var bookedThisWeek = _db.Bookings
            .Where(b => b.Status == BookingStatus.Active
                        && b.Lesson!.Start >= weekStart && b.Lesson.Start < weekEnd
                        && b.Lesson.Status != LessonStatus.Cancelled)
            .Select(b => b.StudentId)
            .ToList()
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());

        var demand = new Dictionary<(LicenseCategory, Transmission), int>();
        foreach (var s in students)
        {
            int remainingLessons = (int)Math.Ceiling(
                DrivingHoursCalculator.RemainingMinutes(_db, s) / (double)DrivingLesson.DefaultDuration);
            bookedThisWeek.TryGetValue(s.Id, out int booked);
            int weekly = Math.Min(remainingLessons + booked, _options.MaxDrivingLessonsPerStudentPerWeek);
            var key = (s.Category, s.PreferredTransmission);
            demand[key] = (demand.TryGetValue(key, out int d) ? d : 0) + weekly;
        }
        return demand;
    }

    private void Accept(Lesson lesson)
    {
        _db.Lessons.Add(lesson);
        _weekLessons.Add(lesson);
        _load[lesson.InstructorId] = Load(lesson.InstructorId) + 1;
    }
}
