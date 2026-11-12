namespace DrivingSchool.Core.Models;

/// <summary>Теоретичне заняття для навчальної групи в аудиторії.</summary>
public class TheoryLesson : Lesson
{
    public const int DefaultDuration = 90;

    public int GroupId { get; set; }
    public StudyGroup? Group { get; set; }

    public string Classroom { get; set; } = string.Empty;

    public override LessonKind Kind => LessonKind.Theory;

    public override int Capacity => 30;

    public override string Location => Classroom;
}
