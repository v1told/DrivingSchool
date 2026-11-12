using DrivingSchool.Core.Common;
using DrivingSchool.Core.Data;
using DrivingSchool.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DrivingSchool.Core.Scheduling;

/// <summary>
/// Сервіс ручного керування розкладом: перегляд, додавання, перенесення
/// і скасування занять з обов'язковою перевіркою конфліктів.
/// </summary>
public class ScheduleService
{
    private readonly AppDbContext _db;
    private readonly ScheduleConflictChecker _checker;

    public ScheduleService(AppDbContext db, ScheduleConflictChecker? checker = null)
    {
        _db = db;
        _checker = checker ?? ScheduleConflictChecker.CreateDefault();
    }

    /// <summary>Повертає заняття за період з можливістю фільтрації.</summary>
    public List<Lesson> GetLessons(DateTime from, DateTime to,
        int? instructorId = null, int? vehicleId = null, int? groupId = null)
    {
        var lessons = _db.Lessons
            .Include(l => l.Instructor)
            .Where(l => l.Start >= from && l.Start < to)
            .ToList();

        // Навігаційні властивості нащадків довантажуємо окремо.
        var drivingIds = lessons.OfType<DrivingLesson>().Select(l => l.Id).ToList();
        _db.DrivingLessons.Where(l => drivingIds.Contains(l.Id))
            .Include(l => l.Vehicle)
            .Include(l => l.Bookings).ThenInclude(b => b.Student)
            .Load();
        var theoryIds = lessons.OfType<TheoryLesson>().Select(l => l.Id).ToList();
        _db.TheoryLessons.Where(l => theoryIds.Contains(l.Id))
            .Include(l => l.Group)
            .Load();

        IEnumerable<Lesson> query = lessons;
        if (instructorId.HasValue)
        {
            query = query.Where(l => l.InstructorId == instructorId.Value);
        }
        if (vehicleId.HasValue)
        {
            query = query.Where(l => l is DrivingLesson d && d.VehicleId == vehicleId.Value);
        }
        if (groupId.HasValue)
        {
            query = query.Where(l => l is TheoryLesson t && t.GroupId == groupId.Value);
        }
        return query.OrderBy(l => l.Start).ToList();
    }

    /// <summary>Перевіряє заняття без збереження і повертає перелік конфліктів.</summary>
    public IReadOnlyList<ScheduleConflict> Validate(Lesson candidate)
    {
        var instructor = _db.Instructors.Find(candidate.InstructorId)
            ?? throw new DomainException("Викладача не знайдено.");
        Vehicle? vehicle = candidate is DrivingLesson d ? _db.Vehicles.Find(d.VehicleId) : null;

        var dayStart = candidate.Start.Date;
        var existing = _db.Lessons
            .Where(l => l.Start >= dayStart && l.Start < dayStart.AddDays(1)
                        && l.Status != LessonStatus.Cancelled)
            .AsNoTracking()
            .ToList();

        var ctx = new ScheduleContext(candidate, instructor, existing) { Vehicle = vehicle };
        return _checker.FindConflicts(ctx);
    }

    public TheoryLesson AddTheoryLesson(int groupId, int instructorId, DateTime start,
        string topic, int duration = TheoryLesson.DefaultDuration, string? classroom = null)
    {
        var group = _db.Groups.Find(groupId) ?? throw new DomainException("Групу не знайдено.");
        var lesson = new TheoryLesson
        {
            GroupId = groupId,
            InstructorId = instructorId,
            Start = start,
            DurationMinutes = duration,
            Topic = topic,
            Classroom = classroom ?? group.Classroom
        };
        Save(lesson);
        return lesson;
    }

    public DrivingLesson AddDrivingSlot(int instructorId, int vehicleId, DateTime start,
        int duration = DrivingLesson.DefaultDuration, string topic = "Практичне водіння")
    {
        var lesson = new DrivingLesson
        {
            InstructorId = instructorId,
            VehicleId = vehicleId,
            Start = start,
            DurationMinutes = duration,
            Topic = topic
        };
        Save(lesson);
        return lesson;
    }

    /// <summary>Переносить заняття на інший час, якщо новий час не створює конфліктів.</summary>
    public void MoveLesson(int lessonId, DateTime newStart)
    {
        var lesson = _db.Lessons.Find(lessonId) ?? throw new DomainException("Заняття не знайдено.");
        if (lesson.Status != LessonStatus.Planned)
        {
            throw new DomainException("Переносити можна лише заплановані заняття.");
        }
        var oldStart = lesson.Start;
        lesson.Start = newStart;
        var conflicts = Validate(lesson);
        if (conflicts.Count > 0)
        {
            lesson.Start = oldStart;
            throw new ScheduleConflictException(conflicts);
        }
        _db.SaveChanges();
    }

    public void CancelLesson(int lessonId)
    {
        var lesson = _db.Lessons.Find(lessonId) ?? throw new DomainException("Заняття не знайдено.");
        if (lesson.Status == LessonStatus.Completed)
        {
            throw new DomainException("Проведене заняття скасувати неможливо.");
        }
        lesson.Status = LessonStatus.Cancelled;
        if (lesson is DrivingLesson)
        {
            foreach (var booking in _db.Bookings.Where(b => b.LessonId == lessonId && b.Status == BookingStatus.Active))
            {
                booking.Status = BookingStatus.Cancelled;
                booking.CancelledAt = DateTime.Now;
            }
        }
        _db.SaveChanges();
    }

    private void Save(Lesson lesson)
    {
        var conflicts = Validate(lesson);
        if (conflicts.Count > 0)
        {
            throw new ScheduleConflictException(conflicts);
        }
        _db.Lessons.Add(lesson);
        _db.SaveChanges();
    }
}
