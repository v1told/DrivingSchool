namespace DrivingSchool.Core.Models;

/// <summary>Категорія посвідчення водія, на яку навчається учень.</summary>
public enum LicenseCategory
{
    A,
    B,
    C
}

/// <summary>Тип коробки передач навчального автомобіля.</summary>
public enum Transmission
{
    Manual,
    Automatic
}

/// <summary>Стан заняття в розкладі.</summary>
public enum LessonStatus
{
    Planned,
    Completed,
    Cancelled
}

/// <summary>Вид заняття (теоретичне або практичне водіння).</summary>
public enum LessonKind
{
    Theory,
    Driving
}

/// <summary>Стан запису учня на практичне заняття.</summary>
public enum BookingStatus
{
    Active,
    Cancelled
}

/// <summary>Відмітка відвідування заняття.</summary>
public enum AttendanceStatus
{
    Present,
    Late,
    Absent,
    Excused
}

/// <summary>Вид оцінювання навчальних досягнень.</summary>
public enum GradeType
{
    TheoryTest,
    DrivingSkill,
    InternalTheoryExam,
    InternalDrivingExam
}
