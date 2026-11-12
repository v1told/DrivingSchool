using DrivingSchool.Core.Common;

namespace DrivingSchool.Core.Models;

/// <summary>Навчальний транспортний засіб.</summary>
public class Vehicle : IEntity
{
    public int Id { get; set; }

    public string Model { get; set; } = string.Empty;

    public string PlateNumber { get; set; } = string.Empty;

    public LicenseCategory Category { get; set; } = LicenseCategory.B;

    public Transmission Transmission { get; set; } = Transmission.Manual;

    /// <summary>false, якщо автомобіль на ремонті або техобслуговуванні.</summary>
    public bool IsAvailable { get; set; } = true;

    public string DisplayName => $"{Model} ({PlateNumber})";

    public string TransmissionName => Transmission == Transmission.Manual ? "Механіка" : "Автомат";

    public override string ToString() => DisplayName;
}
