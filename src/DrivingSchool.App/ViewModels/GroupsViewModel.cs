using DrivingSchool.Core.Models;
using DrivingSchool.Core.Scheduling;

namespace DrivingSchool.App.ViewModels;

/// <summary>Облік навчальних груп.</summary>
public class GroupsViewModel : CrudViewModel<StudyGroup>
{
    public GroupsViewModel()
    {
        Initialize();
    }

    protected override string EntityName => "Група";

    protected override IEnumerable<StudyGroup> LoadItems(ResourceService service) => service.GetGroups();

    protected override StudyGroup CreateNew() => new()
    {
        StartDate = DateTime.Today,
        EndDate = DateTime.Today.AddMonths(3)
    };

    protected override StudyGroup Clone(StudyGroup g) => new()
    {
        Id = g.Id,
        Name = g.Name,
        Category = g.Category,
        StartDate = g.StartDate,
        EndDate = g.EndDate,
        Classroom = g.Classroom
    };

    protected override void SaveItem(ResourceService service, StudyGroup item) => service.SaveGroup(item);
}
