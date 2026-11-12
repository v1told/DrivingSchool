namespace DrivingSchool.Core.Models;

/// <summary>Викладач теорії та (або) інструктор з практичного водіння.</summary>
public class Instructor : Person
{
    public bool TeachesTheory { get; set; }

    public bool TeachesDriving { get; set; } = true;

    public int ExperienceYears { get; set; }

    /// <summary>Закріплений за інструктором навчальний автомобіль.</summary>
    public int? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public List<Lesson> Lessons { get; set; } = new();

    public override string RoleName =>
        TeachesTheory && TeachesDriving ? "Викладач та інструктор"
        : TeachesTheory ? "Викладач теорії"
        : "Інструктор з водіння";
}
