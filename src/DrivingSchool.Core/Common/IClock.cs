namespace DrivingSchool.Core.Common;

/// <summary>
/// Абстракція поточного часу. Дає змогу підставляти фіксований час
/// у модульних тестах замість DateTime.Now.
/// </summary>
public interface IClock
{
    DateTime Now { get; }
}

/// <summary>Реалізація годинника, що повертає системний час.</summary>
public class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
}

/// <summary>Годинник із заданим часом (для тестів і демонстрації).</summary>
public class FixedClock : IClock
{
    public FixedClock(DateTime now)
    {
        Now = now;
    }

    public DateTime Now { get; set; }
}
