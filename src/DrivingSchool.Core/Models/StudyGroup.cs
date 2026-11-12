using DrivingSchool.Core.Common;

namespace DrivingSchool.Core.Models;

/// <summary>Навчальна група для теоретичних занять.</summary>
public class StudyGroup : IEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public LicenseCategory Category { get; set; } = LicenseCategory.B;

    public DateTime StartDate { get; set; } = DateTime.Today;

    public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(3);

    public string Classroom { get; set; } = "Ауд. 1";

    public List<Student> Students { get; set; } = new();

    public bool IsRunningOn(DateTime date) => date.Date >= StartDate.Date && date.Date <= EndDate.Date;

    public override string ToString() => Name;
}
