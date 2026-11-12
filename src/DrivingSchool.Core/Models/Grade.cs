using DrivingSchool.Core.Common;

namespace DrivingSchool.Core.Models;

/// <summary>Оцінка учня за 12-бальною шкалою.</summary>
public class Grade : IEntity
{
    public const int MinScore = 1;
    public const int MaxScore = 12;

    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student? Student { get; set; }

    public int? LessonId { get; set; }
    public Lesson? Lesson { get; set; }

    public GradeType Type { get; set; }

    public int Score { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    public string? Comment { get; set; }

    public string TypeName => Type switch
    {
        GradeType.TheoryTest => "Тест з теорії",
        GradeType.DrivingSkill => "Навички водіння",
        GradeType.InternalTheoryExam => "Внутрішній іспит (теорія)",
        _ => "Внутрішній іспит (водіння)"
    };
}
