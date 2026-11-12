using DrivingSchool.Core.Data;
using DrivingSchool.Core.Models;

namespace DrivingSchool.Core.Common;

/// <summary>
/// Єдине правило обліку годин практичного водіння для обох модулів:
/// відпрацьованими вважаються лише проведені (Completed) практичні заняття,
/// на яких учень мав відмітку «присутній» або «запізнився»;
/// запланованими вважаються активні записи на ще не проведені заняття.
/// </summary>
public static class DrivingHoursCalculator
{
    /// <summary>Відпрацьовані хвилини водіння.</summary>
    public static int CompletedMinutes(AppDbContext db, int studentId) =>
        db.Attendance
            .Where(a => a.StudentId == studentId
                        && (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late))
            .Join(db.DrivingLessons.Where(l => l.Status == LessonStatus.Completed),
                  a => a.LessonId, l => l.Id, (a, l) => l.DurationMinutes)
            .ToList()
            .Sum();

    /// <summary>Заплановані хвилини: активні записи на заняття, які ще не проведені.</summary>
    public static int PlannedMinutes(AppDbContext db, int studentId) =>
        db.Bookings
            .Where(b => b.StudentId == studentId
                        && b.Status == BookingStatus.Active
                        && b.Lesson!.Status == LessonStatus.Planned)
            .Select(b => b.Lesson!.DurationMinutes)
            .ToList()
            .Sum();

    /// <summary>Залишок хвилин за програмою: програма − відпрацьовано − заплановано (не менше 0).</summary>
    public static int RemainingMinutes(AppDbContext db, Student student) =>
        Math.Max(0, student.RequiredDrivingHours * 60
                    - CompletedMinutes(db, student.Id)
                    - PlannedMinutes(db, student.Id));
}
