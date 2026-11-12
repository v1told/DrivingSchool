using DrivingSchool.Core.Common;
using DrivingSchool.Core.Data;
using DrivingSchool.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DrivingSchool.Core.Learning;

/// <summary>Рядок журналу: учень і його відмітка на занятті.</summary>
public class AttendanceEntry
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    public string? Note { get; set; }
}

/// <summary>
/// Облік відвідувань і оцінювання. Журнал формується автоматично:
/// для теорії це всі активні учні групи, для водіння записаний учень.
/// </summary>
public class AttendanceService
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;

    public AttendanceService(AppDbContext db, IClock? clock = null)
    {
        _db = db;
        _clock = clock ?? new SystemClock();
    }

    /// <summary>Заняття, які вже завершилися і потребують (або мають) відмітки.</summary>
    public List<Lesson> GetLessonsForJournal(DateTime from, DateTime to)
    {
        var now = _clock.Now;
        var lessons = _db.Lessons
            .Include(l => l.Instructor)
            .Where(l => l.Start >= from && l.Start < to && l.Start <= now && l.Status != LessonStatus.Cancelled)
            .OrderBy(l => l.Start)
            .ToList()
            .Where(l => l.End <= now) // End обчислюється в пам'яті
            .ToList();
        // Для водіння показуємо лише заняття, на які був запис.
        var bookedIds = _db.Bookings.Where(b => b.Status == BookingStatus.Active)
            .Select(b => b.LessonId).ToHashSet();
        _db.TheoryLessons.Include(l => l.Group).Where(l => l.Start >= from && l.Start < to).Load();
        _db.DrivingLessons.Include(l => l.Vehicle)
            .Include(l => l.Bookings).ThenInclude(b => b.Student)
            .Where(l => l.Start >= from && l.Start < to).Load();
        return lessons.Where(l => l is TheoryLesson || bookedIds.Contains(l.Id)).ToList();
    }

    /// <summary>Формує журнал заняття з урахуванням уже внесених відміток.</summary>
    public List<AttendanceEntry> GetJournal(int lessonId)
    {
        var lesson = _db.Lessons.Find(lessonId) ?? throw new DomainException("Заняття не знайдено.");
        var students = GetExpectedStudents(lesson);
        var marks = _db.Attendance.Where(a => a.LessonId == lessonId)
            .ToDictionary(a => a.StudentId);

        return students.Select(s => new AttendanceEntry
        {
            StudentId = s.Id,
            StudentName = s.FullName,
            Status = marks.TryGetValue(s.Id, out var m) ? m.Status : AttendanceStatus.Present,
            Note = marks.TryGetValue(s.Id, out var n) ? n.Note : null
        }).ToList();
    }

    /// <summary>Зберігає відмітки відвідування і переводить заняття в стан «Проведено».</summary>
    public void SaveJournal(int lessonId, IEnumerable<AttendanceEntry> entries)
    {
        var lesson = _db.Lessons.Find(lessonId) ?? throw new DomainException("Заняття не знайдено.");
        if (lesson.Status == LessonStatus.Cancelled)
        {
            throw new DomainException("Заняття скасовано, відмітки не вносяться.");
        }
        if (lesson.End > _clock.Now)
        {
            throw new DomainException($"Відмітки вносяться лише після завершення заняття (о {lesson.End:HH:mm}).");
        }

        var allowed = GetExpectedStudents(lesson).Select(s => s.Id).ToHashSet();
        var existing = _db.Attendance.Where(a => a.LessonId == lessonId).ToList();

        foreach (var entry in entries)
        {
            if (!allowed.Contains(entry.StudentId))
            {
                throw new DomainException($"Учень {entry.StudentName} не належить до цього заняття.");
            }
            var record = existing.FirstOrDefault(a => a.StudentId == entry.StudentId);
            if (record is null)
            {
                _db.Attendance.Add(new AttendanceRecord
                {
                    LessonId = lessonId,
                    StudentId = entry.StudentId,
                    Status = entry.Status,
                    Note = entry.Note
                });
            }
            else
            {
                record.Status = entry.Status;
                record.Note = entry.Note;
            }
        }

        lesson.Status = LessonStatus.Completed;
        _db.SaveChanges();
    }

    /// <summary>Додає оцінку учню за 12-бальною шкалою.</summary>
    public Grade AddGrade(int studentId, GradeType type, int score, int? lessonId = null, string? comment = null)
    {
        if (score < Grade.MinScore || score > Grade.MaxScore)
        {
            throw new DomainException($"Оцінка має бути в межах {Grade.MinScore}–{Grade.MaxScore}.");
        }
        if (_db.Students.Find(studentId) is null)
        {
            throw new DomainException("Учня не знайдено.");
        }
        var grade = new Grade
        {
            StudentId = studentId,
            Type = type,
            Score = score,
            LessonId = lessonId,
            Comment = comment,
            Date = _clock.Now.Date
        };
        _db.Grades.Add(grade);
        _db.SaveChanges();
        return grade;
    }

    public List<Grade> GetGrades(int studentId) =>
        _db.Grades.Where(g => g.StudentId == studentId)
            .OrderByDescending(g => g.Date).ToList();

    private List<Student> GetExpectedStudents(Lesson lesson)
    {
        if (lesson is TheoryLesson theory)
        {
            return _db.Students
                .Where(s => s.GroupId == theory.GroupId && s.IsActive)
                .OrderBy(s => s.LastName)
                .ToList();
        }
        return _db.Bookings
            .Where(b => b.LessonId == lesson.Id && b.Status == BookingStatus.Active)
            .Select(b => b.Student!)
            .ToList();
    }
}
