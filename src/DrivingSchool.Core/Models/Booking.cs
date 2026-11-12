using DrivingSchool.Core.Common;

namespace DrivingSchool.Core.Models;

/// <summary>Запис учня на практичне заняття з водіння.</summary>
public class Booking : IEntity
{
    public int Id { get; set; }

    public int LessonId { get; set; }
    public DrivingLesson? Lesson { get; set; }

    public int StudentId { get; set; }
    public Student? Student { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public BookingStatus Status { get; set; } = BookingStatus.Active;

    public DateTime? CancelledAt { get; set; }
}
