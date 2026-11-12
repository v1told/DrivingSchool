using DrivingSchool.Core.Models;

namespace DrivingSchool.Core.Learning;

/// <summary>Зведені показники навчального прогресу учня.</summary>
public class StudentProgress
{
    public int StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string GroupName { get; init; } = string.Empty;

    public int TheoryLessonsHeld { get; init; }
    public int TheoryLessonsAttended { get; init; }

    public double DrivingHoursCompleted { get; init; }
    public int DrivingHoursRequired { get; init; }

    public double? AverageTheoryScore { get; init; }
    public double? AverageDrivingScore { get; init; }

    public int? TheoryExamScore { get; init; }
    public int? DrivingExamScore { get; init; }

    public bool AdmittedToTheoryExam { get; init; }
    public bool TheoryExamPassed { get; init; }
    public bool AdmittedToDrivingExam { get; init; }
    public bool DrivingExamPassed { get; init; }

    public double TheoryAttendancePercent =>
        TheoryLessonsHeld == 0 ? 0 : Math.Round(100.0 * TheoryLessonsAttended / TheoryLessonsHeld, 1);

    public double DrivingPercent =>
        DrivingHoursRequired == 0 ? 0 : Math.Min(100, Math.Round(100.0 * DrivingHoursCompleted / DrivingHoursRequired, 1));

    /// <summary>Загальний відсоток проходження програми (теорія і практика порівну).</summary>
    public double OverallPercent => Math.Round((Math.Min(TheoryAttendancePercent, 100) + DrivingPercent) / 2, 1);

    public string StageText =>
        DrivingExamPassed ? "Курс завершено, готовий до іспиту в ТСЦ"
        : AdmittedToDrivingExam ? "Допущений до внутрішнього іспиту з водіння"
        : TheoryExamPassed ? "Теорію складено, триває практика"
        : AdmittedToTheoryExam ? "Допущений до внутрішнього іспиту з теорії"
        : "Навчання триває";
}
