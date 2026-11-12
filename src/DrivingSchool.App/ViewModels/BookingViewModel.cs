using System.Collections.ObjectModel;
using DrivingSchool.App.Infrastructure;
using DrivingSchool.Core.Common;
using DrivingSchool.Core.Learning;
using DrivingSchool.Core.Models;
using DrivingSchool.Core.Scheduling;

namespace DrivingSchool.App.ViewModels;

/// <summary>Рядок таблиці записів учня.</summary>
public record BookingRow(int BookingId, string Day, string Time, string Instructor, string Vehicle);

/// <summary>
/// Запис учня на практичні заняття: перегляд вільних слотів,
/// запис, перегляд власних записів і скасування.
/// </summary>
public class BookingViewModel : ViewModelBase
{
    private Student? _student;
    private DateTime _weekStart = DateHelper.StartOfWeek(DateTime.Today);
    private LessonRow? _selectedSlot;
    private BookingRow? _selectedBooking;

    public BookingViewModel()
    {
        BookCommand = new RelayCommand(Book, () => Student != null && SelectedSlot != null);
        CancelCommand = new RelayCommand(CancelBooking, () => SelectedBooking != null);
        PrevWeekCommand = new RelayCommand(() => WeekStart = WeekStart.AddDays(-7));
        NextWeekCommand = new RelayCommand(() => WeekStart = WeekStart.AddDays(7));
        LoadStudents();
    }

    public ObservableCollection<Student> Students { get; } = new();
    public ObservableCollection<LessonRow> FreeSlots { get; } = new();
    public ObservableCollection<BookingRow> Bookings { get; } = new();

    public RelayCommand BookCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand PrevWeekCommand { get; }
    public RelayCommand NextWeekCommand { get; }

    public Student? Student
    {
        get => _student;
        set { if (SetProperty(ref _student, value)) Load(); }
    }

    public DateTime WeekStart
    {
        get => _weekStart;
        set
        {
            if (SetProperty(ref _weekStart, DateHelper.StartOfWeek(value)))
            {
                OnPropertyChanged(nameof(WeekTitle));
                Load();
            }
        }
    }

    public string WeekTitle => $"Тиждень {WeekStart:dd.MM.yyyy} – {WeekStart.AddDays(6):dd.MM.yyyy}";

    public LessonRow? SelectedSlot { get => _selectedSlot; set => SetProperty(ref _selectedSlot, value); }
    public BookingRow? SelectedBooking { get => _selectedBooking; set => SetProperty(ref _selectedBooking, value); }

    private string _studentInfo = string.Empty;
    public string StudentInfo { get => _studentInfo; set => SetProperty(ref _studentInfo, value); }

    public void LoadStudents()
    {
        using var db = Db.Create();
        int? currentId = Student?.Id;
        Students.Clear();
        foreach (var s in new ResourceService(db).GetStudents().Where(s => s.IsActive)) Students.Add(s);
        Student = Students.FirstOrDefault(s => s.Id == currentId) ?? Students.FirstOrDefault();
    }

    public void Load()
    {
        FreeSlots.Clear();
        Bookings.Clear();
        if (Student == null) return;

        using var db = Db.Create();
        var service = new BookingService(db);
        foreach (var slot in service.GetAvailableSlots(Student.Id, WeekStart, WeekStart.AddDays(7)))
        {
            FreeSlots.Add(new LessonRow(slot));
        }
        var bookings = service.GetStudentBookings(Student.Id);
        foreach (var b in bookings.Where(b => b.Lesson!.Start >= DateTime.Today))
        {
            var l = b.Lesson!;
            Bookings.Add(new BookingRow(b.Id,
                $"{DateHelper.DayName(l.Start.DayOfWeek)}, {l.Start:dd.MM}",
                $"{l.Start:HH:mm}–{l.End:HH:mm}",
                l.Instructor?.ShortName ?? string.Empty,
                l.Vehicle?.DisplayName ?? string.Empty));
        }

        double done = DrivingHoursCalculator.CompletedMinutes(db, Student.Id) / 60.0;
        double planned = DrivingHoursCalculator.PlannedMinutes(db, Student.Id) / 60.0;
        double left = DrivingHoursCalculator.RemainingMinutes(db, Student) / 60.0;
        StudentInfo = $"Категорія {Student.Category}, {(Student.PreferredTransmission == Transmission.Manual ? "механіка" : "автомат")}. " +
                      $"Програма {Student.RequiredDrivingHours} год: відпрацьовано {done:0.#}, заплановано {planned:0.#}, " +
                      $"залишок для запису {left:0.#} год.";
        Status = $"Вільних слотів на тиждень: {FreeSlots.Count}";
    }

    private void Book()
    {
        if (Student == null || SelectedSlot == null) return;
        int studentId = Student.Id;
        int lessonId = SelectedSlot.Id;
        string when = $"{SelectedSlot.Day} {SelectedSlot.Time}";
        if (Run(() =>
            {
                using var db = Db.Create();
                new BookingService(db).Book(studentId, lessonId);
            }))
        {
            Load();
            Status = $"{Student.ShortName} записаний(а) на {when}. " + Status;
        }
    }

    private void CancelBooking()
    {
        if (SelectedBooking == null || !Confirm($"Скасувати запис на {SelectedBooking.Day} {SelectedBooking.Time}?")) return;
        int id = SelectedBooking.BookingId;
        if (Run(() =>
            {
                using var db = Db.Create();
                new BookingService(db).Cancel(id);
            }))
        {
            Load();
            Status = "Запис скасовано, слот знову вільний. " + Status;
        }
    }
}
