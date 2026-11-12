using System.Collections.ObjectModel;
using DrivingSchool.Core.Models;
using DrivingSchool.Core.Scheduling;

namespace DrivingSchool.App.ViewModels;

/// <summary>Облік учнів автошколи.</summary>
public class StudentsViewModel : CrudViewModel<Student>
{
    private string _search = string.Empty;

    public StudentsViewModel()
    {
        Initialize();
    }

    public ObservableCollection<StudyGroup> Groups { get; } = new();

    /// <summary>Пошук за прізвищем, ім'ям або телефоном (оновлюється під час введення).</summary>
    public string Search
    {
        get => _search;
        set
        {
            if (SetProperty(ref _search, value)) Load();
        }
    }

    protected override string EntityName => "Учень";

    protected override void LoadLookups(ResourceService service)
    {
        Groups.Clear();
        foreach (var g in service.GetGroups()) Groups.Add(g);
    }

    protected override IEnumerable<Student> LoadItems(ResourceService service) => service.GetStudents(Search);

    protected override Student CreateNew() => new()
    {
        BirthDate = DateTime.Today.AddYears(-18),
        GroupId = Groups.FirstOrDefault()?.Id
    };

    protected override Student Clone(Student s) => new()
    {
        Id = s.Id,
        LastName = s.LastName,
        FirstName = s.FirstName,
        MiddleName = s.MiddleName,
        Phone = s.Phone,
        BirthDate = s.BirthDate,
        EnrollmentDate = s.EnrollmentDate,
        Category = s.Category,
        PreferredTransmission = s.PreferredTransmission,
        RequiredDrivingHours = s.RequiredDrivingHours,
        IsActive = s.IsActive,
        GroupId = s.GroupId
    };

    protected override void SaveItem(ResourceService service, Student item) => service.SaveStudent(item);

    protected override string Describe(Student item) => item.FullName;
}
