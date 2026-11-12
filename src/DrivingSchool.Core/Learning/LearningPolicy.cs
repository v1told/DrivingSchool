namespace DrivingSchool.Core.Learning;

/// <summary>
/// Внутрішні правила автошколи щодо запису на заняття та допуску до іспитів.
/// Значення можна змінити відповідно до положення конкретного закладу.
/// </summary>
public class LearningPolicy
{
    /// <summary>Скільки годин до початку заняття ще можна скасувати запис.</summary>
    public int CancellationDeadlineHours { get; set; } = 24;

    /// <summary>Максимум практичних занять учня на день.</summary>
    public int MaxDrivingLessonsPerDay { get; set; } = 2;

    /// <summary>Максимум практичних занять учня на тиждень.</summary>
    public int MaxDrivingLessonsPerWeek { get; set; } = 4;

    /// <summary>Мінімальна відвідуваність теорії (%) для допуску до внутрішнього іспиту.</summary>
    public double MinTheoryAttendancePercent { get; set; } = 80;

    /// <summary>Мінімальний середній бал (з 12) для допуску до іспиту.</summary>
    public double MinAverageScore { get; set; } = 7;

    /// <summary>Мінімальна оцінка (з 12), за якої іспит вважається складеним.</summary>
    public int PassingExamScore { get; set; } = 7;
}
