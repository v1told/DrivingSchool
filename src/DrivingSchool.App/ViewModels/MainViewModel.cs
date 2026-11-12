using DrivingSchool.App.Infrastructure;

namespace DrivingSchool.App.ViewModels;

/// <summary>Головна модель представлення: об'єднує модулі обох розробниць.</summary>
public class MainViewModel : ViewModelBase
{
    // Модуль 1: облік ресурсів і формування розкладу
    public StudentsViewModel Students { get; } = new();
    public InstructorsViewModel Instructors { get; } = new();
    public VehiclesViewModel Vehicles { get; } = new();
    public GroupsViewModel Groups { get; } = new();
    public ScheduleViewModel Schedule { get; } = new();

    // Модуль 2: запис на заняття, відвідування, успішність
    public BookingViewModel Booking { get; } = new();
    public AttendanceViewModel Attendance { get; } = new();
    public ProgressViewModel Progress { get; } = new();

    public void RefreshTab(int index)
    {
        switch (index)
        {
            case 0: Students.Load(); break;
            case 1: Instructors.Load(); break;
            case 2: Vehicles.Load(); break;
            case 3: Groups.Load(); break;
            case 4: Schedule.LoadLookups(); Schedule.Load(); break;
            case 5: Booking.LoadStudents(); Booking.Load(); break;
            case 6: Attendance.Load(); break;
            case 7: Progress.LoadGroups(); Progress.Load(); break;
        }
    }
}
