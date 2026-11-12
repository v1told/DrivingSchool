namespace DrivingSchool.Core.Common;

public static class DateHelper
{
    /// <summary>Повертає понеділок тижня, до якого належить дата.</summary>
    public static DateTime StartOfWeek(DateTime date)
    {
        int diff = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-diff);
    }

    public static string DayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Понеділок",
        DayOfWeek.Tuesday => "Вівторок",
        DayOfWeek.Wednesday => "Середа",
        DayOfWeek.Thursday => "Четвер",
        DayOfWeek.Friday => "П'ятниця",
        DayOfWeek.Saturday => "Субота",
        _ => "Неділя"
    };
}
