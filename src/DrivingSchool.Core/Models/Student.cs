namespace DrivingSchool.Core.Models;

/// <summary>Учень автошколи.</summary>
public class Student : Person
{
    public DateTime BirthDate { get; set; }

    public DateTime EnrollmentDate { get; set; } = DateTime.Today;

    public LicenseCategory Category { get; set; } = LicenseCategory.B;

    /// <summary>Бажаний тип коробки передач для практичного водіння.</summary>
    public Transmission PreferredTransmission { get; set; } = Transmission.Manual;

    /// <summary>
    /// Кількість годин практичного водіння за навчальною програмою
    /// (для категорії B: 40 год на механічній КПП, 38 год на автоматичній).
    /// </summary>
    public int RequiredDrivingHours { get; set; } = 40;

    public bool IsActive { get; set; } = true;

    public int? GroupId { get; set; }
    public StudyGroup? Group { get; set; }

    public List<Booking> Bookings { get; set; } = new();
    public List<AttendanceRecord> Attendance { get; set; } = new();
    public List<Grade> Grades { get; set; } = new();

    public override string RoleName => "Учень";

    public string GroupName => Group?.Name ?? "без групи";
}
