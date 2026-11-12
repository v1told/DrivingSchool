using DrivingSchool.Core.Common;

namespace DrivingSchool.Core.Models;

/// <summary>Відмітка відвідування учнем конкретного заняття.</summary>
public class AttendanceRecord : IEntity
{
    public int Id { get; set; }

    public int LessonId { get; set; }
    public Lesson? Lesson { get; set; }

    public int StudentId { get; set; }
    public Student? Student { get; set; }

    public AttendanceStatus Status { get; set; }

    public string? Note { get; set; }

    /// <summary>Чи зараховується заняття учню (присутній або запізнився).</summary>
    public bool CountsAsAttended => Status is AttendanceStatus.Present or AttendanceStatus.Late;
}
