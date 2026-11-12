using DrivingSchool.Core.Common;
using DrivingSchool.Core.Data;
using DrivingSchool.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DrivingSchool.Core.Learning;

/// <summary>
/// Розрахунок навчального прогресу учня: відвідуваність теорії,
/// відпрацьовані години водіння, середні бали та допуск до
/// внутрішніх іспитів відповідно до правил LearningPolicy.
/// </summary>
public class ProgressService
{
    private readonly AppDbContext _db;
    private readonly LearningPolicy _policy;

    public ProgressService(AppDbContext db, LearningPolicy? policy = null)
    {
        _db = db;
        _policy = policy ?? new LearningPolicy();
    }

    public StudentProgress GetProgress(int studentId)
    {
        var student = _db.Students.Include(s => s.Group).AsNoTracking()
            .FirstOrDefault(s => s.Id == studentId)
            ?? throw new DomainException("Учня не знайдено.");

        // Теорія: проведені заняття групи та зараховані відвідування.
        int theoryHeld = 0;
        int theoryAttended = 0;
        if (student.GroupId.HasValue)
        {
            var heldIds = _db.TheoryLessons
                .Where(l => l.GroupId == student.GroupId && l.Status == LessonStatus.Completed)
                .Select(l => l.Id)
                .ToList();
            theoryHeld = heldIds.Count;
            theoryAttended = _db.Attendance
                .Where(a => a.StudentId == studentId && heldIds.Contains(a.LessonId))
                .AsEnumerable()
                .Count(a => a.CountsAsAttended);
        }

        // Практика: лише проведені заняття водіння з відміткою присутності.
        double drivingMinutes = DrivingHoursCalculator.CompletedMinutes(_db, studentId);

        var grades = _db.Grades.Where(g => g.StudentId == studentId).AsNoTracking().ToList();
        double? avgTheory = Average(grades, GradeType.TheoryTest);
        double? avgDriving = Average(grades, GradeType.DrivingSkill);
        int? theoryExam = LastScore(grades, GradeType.InternalTheoryExam);
        int? drivingExam = LastScore(grades, GradeType.InternalDrivingExam);

        double attendancePercent = theoryHeld == 0 ? 0 : 100.0 * theoryAttended / theoryHeld;
        double drivingHours = drivingMinutes / 60.0;

        bool admittedTheory = theoryHeld > 0
                              && attendancePercent >= _policy.MinTheoryAttendancePercent
                              && (avgTheory ?? 0) >= _policy.MinAverageScore;
        bool theoryPassed = admittedTheory && theoryExam >= _policy.PassingExamScore;
        bool admittedDriving = theoryPassed
                               && drivingHours >= student.RequiredDrivingHours
                               && (avgDriving ?? 0) >= _policy.MinAverageScore;
        bool drivingPassed = admittedDriving && drivingExam >= _policy.PassingExamScore;

        return new StudentProgress
        {
            StudentId = student.Id,
            StudentName = student.FullName,
            GroupName = student.GroupName,
            TheoryLessonsHeld = theoryHeld,
            TheoryLessonsAttended = theoryAttended,
            DrivingHoursCompleted = Math.Round(drivingHours, 1),
            DrivingHoursRequired = student.RequiredDrivingHours,
            AverageTheoryScore = avgTheory,
            AverageDrivingScore = avgDriving,
            TheoryExamScore = theoryExam,
            DrivingExamScore = drivingExam,
            AdmittedToTheoryExam = admittedTheory,
            TheoryExamPassed = theoryPassed,
            AdmittedToDrivingExam = admittedDriving,
            DrivingExamPassed = drivingPassed
        };
    }

    /// <summary>Прогрес усіх учнів групи (або всіх учнів, якщо групу не задано).</summary>
    public List<StudentProgress> GetGroupProgress(int? groupId)
    {
        var ids = _db.Students
            .Where(s => groupId == null || s.GroupId == groupId)
            .OrderBy(s => s.LastName)
            .Select(s => s.Id)
            .ToList();
        return ids.Select(GetProgress).ToList();
    }

    private static double? Average(List<Grade> grades, GradeType type)
    {
        var scores = grades.Where(g => g.Type == type).Select(g => g.Score).ToList();
        return scores.Count == 0 ? null : Math.Round(scores.Average(), 1);
    }

    private static int? LastScore(List<Grade> grades, GradeType type) =>
        grades.Where(g => g.Type == type)
            .OrderByDescending(g => g.Date).ThenByDescending(g => g.Id)
            .Select(g => (int?)g.Score)
            .FirstOrDefault();
}
