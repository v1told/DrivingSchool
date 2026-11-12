using DrivingSchool.Core.Common;
using DrivingSchool.Core.Data;
using DrivingSchool.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DrivingSchool.Core.Learning;

/// <summary>
/// Запис учнів на практичні заняття з водіння та скасування запису.
/// Один і той самий набір правил (CheckRules) використовується і для
/// показу доступних слотів, і для самого запису, тому учню ніколи
/// не пропонується слот, на який його не можна записати.
/// </summary>
public class BookingService
{
    private readonly AppDbContext _db;
    private readonly LearningPolicy _policy;
    private readonly IClock _clock;

    public BookingService(AppDbContext db, LearningPolicy? policy = null, IClock? clock = null)
    {
        _db = db;
        _policy = policy ?? new LearningPolicy();
        _clock = clock ?? new SystemClock();
    }

    /// <summary>
    /// Слоти, на які учень може записатися в заданому періоді: з урахуванням
    /// категорії, коробки передач, накладок, денного і тижневого лімітів
    /// та залишку годин за програмою.
    /// </summary>
    public List<DrivingLesson> GetAvailableSlots(int studentId, DateTime from, DateTime to)
    {
        var student = GetStudent(studentId);
        if (!student.IsActive)
        {
            return new List<DrivingLesson>();
        }

        var now = _clock.Now;
        var slots = _db.DrivingLessons
            .Include(l => l.Vehicle)
            .Include(l => l.Instructor)
            .Include(l => l.Bookings)
            .Where(l => l.Start >= from && l.Start < to && l.Start > now
                        && l.Status == LessonStatus.Planned)
            .OrderBy(l => l.Start)
            .ToList();

        int remaining = DrivingHoursCalculator.RemainingMinutes(_db, student);
        var weekCache = new Dictionary<DateTime, List<Lesson>>();
        var result = new List<DrivingLesson>();

        foreach (var slot in slots)
        {
            var weekStart = DateHelper.StartOfWeek(slot.Start);
            if (!weekCache.TryGetValue(weekStart, out var weekLessons))
            {
                weekLessons = GetStudentLessons(student, weekStart, weekStart.AddDays(7));
                weekCache[weekStart] = weekLessons;
            }
            if (CheckRules(student, slot, weekLessons, remaining) is null)
            {
                result.Add(slot);
            }
        }
        return result;
    }

    /// <summary>Записує учня на практичне заняття.</summary>
    public Booking Book(int studentId, int lessonId)
    {
        var student = GetStudent(studentId);
        if (!student.IsActive)
        {
            throw new DomainException("Учень не навчається (неактивний), запис неможливий.");
        }

        var lesson = _db.DrivingLessons
            .Include(l => l.Vehicle)
            .Include(l => l.Bookings)
            .FirstOrDefault(l => l.Id == lessonId)
            ?? throw new DomainException("Практичне заняття не знайдено.");

        var weekStart = DateHelper.StartOfWeek(lesson.Start);
        var weekLessons = GetStudentLessons(student, weekStart, weekStart.AddDays(7));
        int remaining = DrivingHoursCalculator.RemainingMinutes(_db, student);

        string? error = CheckRules(student, lesson, weekLessons, remaining);
        if (error is not null)
        {
            throw new DomainException(error);
        }

        var booking = new Booking
        {
            LessonId = lesson.Id,
            StudentId = studentId,
            CreatedAt = _clock.Now,
            Status = BookingStatus.Active
        };
        _db.Bookings.Add(booking);
        _db.SaveChanges();
        return booking;
    }

    /// <summary>
    /// Перевіряє всі правила запису. Повертає текст першого порушення
    /// або null, якщо запис можливий.
    /// </summary>
    private string? CheckRules(Student student, DrivingLesson lesson, List<Lesson> weekLessons, int remainingMinutes)
    {
        if (lesson.Status != LessonStatus.Planned)
            return "Записатися можна лише на заплановане заняття.";
        if (lesson.Start <= _clock.Now)
            return "Неможливо записатися на заняття, яке вже почалося або минуло.";
        if (!lesson.IsFree)
            return "На це заняття вже записаний інший учень.";
        if (lesson.Vehicle is null || lesson.Vehicle.Category != student.Category)
            return "Категорія автомобіля не відповідає категорії навчання учня.";
        if (lesson.Vehicle.Transmission != student.PreferredTransmission)
            return "Тип коробки передач не відповідає обраному учнем.";
        if (weekLessons.Any(l => l.OverlapsWith(lesson)))
            return "У цей час учень уже має інше заняття.";

        var driving = weekLessons.OfType<DrivingLesson>().ToList();
        if (driving.Count(l => l.Start.Date == lesson.Start.Date) >= _policy.MaxDrivingLessonsPerDay)
            return $"Перевищено ліміт: не більше {_policy.MaxDrivingLessonsPerDay} занять водіння на день.";
        if (driving.Count >= _policy.MaxDrivingLessonsPerWeek)
            return $"Перевищено ліміт: не більше {_policy.MaxDrivingLessonsPerWeek} занять водіння на тиждень.";
        if (lesson.DurationMinutes > remainingMinutes)
            return "Години водіння за програмою вже відпрацьовані або заплановані повністю.";
        return null;
    }

    /// <summary>Скасовує запис, якщо до початку заняття лишилося достатньо часу.</summary>
    public void Cancel(int bookingId)
    {
        var booking = _db.Bookings.Include(b => b.Lesson)
            .FirstOrDefault(b => b.Id == bookingId)
            ?? throw new DomainException("Запис не знайдено.");

        if (booking.Status == BookingStatus.Cancelled)
        {
            throw new DomainException("Запис уже скасовано.");
        }
        if (booking.Lesson!.Start - _clock.Now < TimeSpan.FromHours(_policy.CancellationDeadlineHours))
        {
            throw new DomainException($"Скасувати запис можна не пізніше ніж за {_policy.CancellationDeadlineHours} год до початку заняття.");
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = _clock.Now;
        _db.SaveChanges();
    }

    /// <summary>Усі активні записи учня (для особистого розкладу).</summary>
    public List<Booking> GetStudentBookings(int studentId) =>
        _db.Bookings
            .Include(b => b.Lesson).ThenInclude(l => l!.Instructor)
            .Include(b => b.Lesson).ThenInclude(l => l!.Vehicle)
            .Where(b => b.StudentId == studentId && b.Status == BookingStatus.Active)
            .OrderBy(b => b.Lesson!.Start)
            .ToList();

    private Student GetStudent(int studentId) =>
        _db.Students.Find(studentId) ?? throw new DomainException("Учня не знайдено.");

    /// <summary>Теоретичні заняття групи учня та його записи на водіння за період.</summary>
    private List<Lesson> GetStudentLessons(Student student, DateTime from, DateTime to)
    {
        var result = new List<Lesson>();
        if (student.GroupId.HasValue)
        {
            result.AddRange(_db.TheoryLessons
                .Where(l => l.GroupId == student.GroupId && l.Start >= from && l.Start < to
                            && l.Status != LessonStatus.Cancelled)
                .ToList());
        }
        result.AddRange(_db.Bookings
            .Where(b => b.StudentId == student.Id && b.Status == BookingStatus.Active
                        && b.Lesson!.Start >= from && b.Lesson.Start < to
                        && b.Lesson.Status != LessonStatus.Cancelled)
            .Select(b => b.Lesson!)
            .ToList());
        return result;
    }
}
