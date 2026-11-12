using DrivingSchool.Core.Models;
using DrivingSchool.Core.Scheduling;

namespace DrivingSchool.App.ViewModels;

/// <summary>Облік навчальних автомобілів.</summary>
public class VehiclesViewModel : CrudViewModel<Vehicle>
{
    public VehiclesViewModel()
    {
        Initialize();
    }

    protected override string EntityName => "Автомобіль";

    protected override IEnumerable<Vehicle> LoadItems(ResourceService service) => service.GetVehicles();

    protected override Vehicle CreateNew() => new();

    protected override Vehicle Clone(Vehicle v) => new()
    {
        Id = v.Id,
        Model = v.Model,
        PlateNumber = v.PlateNumber,
        Category = v.Category,
        Transmission = v.Transmission,
        IsAvailable = v.IsAvailable
    };

    protected override void SaveItem(ResourceService service, Vehicle item) => service.SaveVehicle(item);
}
