namespace DrivingSchool.Core.Scheduling;

/// <summary>Параметри автоматичного формування тижневого розкладу.</summary>
public class ScheduleGenerationOptions
{
    /// <summary>Кількість теоретичних занять для кожної групи на тиждень.</summary>
    public int TheoryLessonsPerWeek { get; set; } = 3;

    public DayOfWeek[] TheoryDays { get; set; } =
        { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

    /// <summary>Можливий час початку теорії в порядку пріоритету.</summary>
    public TimeSpan[] TheoryStartTimes { get; set; } =
        { new(18, 0, 0), new(16, 15, 0), new(10, 0, 0) };

    public DayOfWeek[] DrivingDays { get; set; } =
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday
    };

    public TimeSpan[] DrivingStartTimes { get; set; } =
    {
        new(8, 0, 0), new(9, 0, 0), new(10, 0, 0), new(11, 0, 0),
        new(13, 0, 0), new(14, 0, 0), new(15, 0, 0), new(16, 0, 0), new(17, 0, 0)
    };

    /// <summary>Максимум практичних занять одного учня на тиждень (для розрахунку попиту).</summary>
    public int MaxDrivingLessonsPerStudentPerWeek { get; set; } = 3;

    /// <summary>Слоти раніше цього моменту не створюються (наприклад, поточний час).</summary>
    public DateTime? NotBefore { get; set; }

    /// <summary>Максимум практичних занять одного інструктора на день.</summary>
    public int MaxDrivingSlotsPerInstructorPerDay { get; set; } = 6;
}
