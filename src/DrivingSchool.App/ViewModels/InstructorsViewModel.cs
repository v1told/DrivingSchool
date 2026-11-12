using System.Collections.ObjectModel;
using DrivingSchool.Core.Models;
using DrivingSchool.Core.Scheduling;

namespace DrivingSchool.App.ViewModels;

/// <summary>Облік викладачів та інструкторів.</summary>
public class InstructorsViewModel : CrudViewModel<Instructor>
{
    public InstructorsViewModel()
    {
        Initialize();
    }

    public ObservableCollection<Vehicle> Vehicles { get; } = new();

    protected override string EntityName => "Інструктор";

    protected override void LoadLookups(ResourceService service)
    {
        Vehicles.Clear();
        foreach (var v in service.GetVehicles()) Vehicles.Add(v);
    }

    protected override IEnumerable<Instructor> LoadItems(ResourceService service) => service.GetInstructors();

    protected override Instructor CreateNew() => new() { TeachesDriving = true };

    protected override Instructor Clone(Instructor s) => new()
    {
        Id = s.Id,
        LastName = s.LastName,
        FirstName = s.FirstName,
        MiddleName = s.MiddleName,
        Phone = s.Phone,
        TeachesTheory = s.TeachesTheory,
        TeachesDriving = s.TeachesDriving,
        ExperienceYears = s.ExperienceYears,
        VehicleId = s.VehicleId
    };

    protected override void SaveItem(ResourceService service, Instructor item) => service.SaveInstructor(item);

    protected override string Describe(Instructor item) => item.FullName;
}
