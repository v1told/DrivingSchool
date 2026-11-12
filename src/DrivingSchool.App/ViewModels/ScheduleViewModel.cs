using System.Collections.ObjectModel;
using System.Globalization;
using DrivingSchool.App.Infrastructure;
using DrivingSchool.Core.Common;
using DrivingSchool.Core.Models;
using DrivingSchool.Core.Scheduling;

namespace DrivingSchool.App.ViewModels;

/// <summary>
/// Формування і перегляд розкладу: автоматична генерація тижня,
/// ручне додавання, перенесення та скасування занять, фільтри.
/// </summary>
public class ScheduleViewModel : ViewModelBase
{
    private DateTime _weekStart = DateHelper.StartOfWeek(DateTime.Today);
    private LessonRow? _selected;
    private Instructor? _filterInstructor;
    private StudyGroup? _filterGroup;

    // Поля форми нового заняття
    private LessonKind _newKind = LessonKind.Theory;
    private DateTime _newDate = DateTime.Today.AddDays(1);
    private string _newTime = "18:00";
    private Instructor? _newInstructor;
    private StudyGroup? _newGroup;
    private string _newTopic = "Теоретичний курс";
    private string _moveTime = "10:00";
    private DateTime _moveDate = DateTime.Today.AddDays(1);

    public ScheduleViewModel()
    {
        PrevWeekCommand = new RelayCommand(() => WeekStart = WeekStart.AddDays(-7));
        NextWeekCommand = new RelayCommand(() => WeekStart = WeekStart.AddDays(7));
        TodayCommand = new RelayCommand(() => WeekStart = DateHelper.StartOfWeek(DateTime.Today));
        GenerateCommand = new RelayCommand(Generate);
        AddCommand = new RelayCommand(Add);
        MoveCommand = new RelayCommand(Move, () => Selected != null);
        CancelCommand = new RelayCommand(Cancel, () => Selected != null);
        ClearFiltersCommand = new RelayCommand(() => { _filterInstructor = null; _filterGroup = null; OnPropertyChanged(nameof(FilterInstructor)); OnPropertyChanged(nameof(FilterGroup)); Load(); });
        LoadLookups();
        Load();
    }

    public ObservableCollection<LessonRow> Lessons { get; } = new();
    public ObservableCollection<Instructor> Instructors { get; } = new();
    public ObservableCollection<StudyGroup> Groups { get; } = new();

    public RelayCommand PrevWeekCommand { get; }
    public RelayCommand NextWeekCommand { get; }
    public RelayCommand TodayCommand { get; }
    public RelayCommand GenerateCommand { get; }
    public RelayCommand AddCommand { get; }
    public RelayCommand MoveCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand ClearFiltersCommand { get; }

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

    public LessonRow? Selected
    {
        get => _selected;
        set
        {
            if (SetProperty(ref _selected, value) && value != null)
            {
                MoveDate = value.Start.Date;
                MoveTime = value.Start.ToString("HH:mm");
            }
        }
    }

    public Instructor? FilterInstructor
    {
        get => _filterInstructor;
        set { if (SetProperty(ref _filterInstructor, value)) Load(); }
    }

    public StudyGroup? FilterGroup
    {
        get => _filterGroup;
        set { if (SetProperty(ref _filterGroup, value)) Load(); }
    }

    public LessonKind NewKind
    {
        get => _newKind;
        set
        {
            if (SetProperty(ref _newKind, value))
            {
                NewTopic = value == LessonKind.Theory ? "Теоретичний курс" : "Практичне водіння";
                NewTime = value == LessonKind.Theory ? "18:00" : "10:00";
                OnPropertyChanged(nameof(IsTheory));
            }
        }
    }

    public bool IsTheory => NewKind == LessonKind.Theory;

    public DateTime NewDate { get => _newDate; set => SetProperty(ref _newDate, value); }
    public string NewTime { get => _newTime; set => SetProperty(ref _newTime, value); }
    public Instructor? NewInstructor { get => _newInstructor; set => SetProperty(ref _newInstructor, value); }
    public StudyGroup? NewGroup { get => _newGroup; set => SetProperty(ref _newGroup, value); }
    public string NewTopic { get => _newTopic; set => SetProperty(ref _newTopic, value); }
    public DateTime MoveDate { get => _moveDate; set => SetProperty(ref _moveDate, value); }
    public string MoveTime { get => _moveTime; set => SetProperty(ref _moveTime, value); }

    public void LoadLookups()
    {
        using var db = Db.Create();
        var resources = new ResourceService(db);
        Instructors.Clear();
        foreach (var i in resources.GetInstructors()) Instructors.Add(i);
        Groups.Clear();
        foreach (var g in resources.GetGroups()) Groups.Add(g);
    }

    public void Load()
    {
        using var db = Db.Create();
        var lessons = new ScheduleService(db).GetLessons(
            WeekStart, WeekStart.AddDays(7), FilterInstructor?.Id, null, FilterGroup?.Id);
        Lessons.Clear();
        foreach (var l in lessons) Lessons.Add(new LessonRow(l));
        int theory = lessons.Count(l => l.Kind == LessonKind.Theory);
        int free = lessons.OfType<DrivingLesson>().Count(l => l.IsFree);
        Status = $"Занять: {lessons.Count} (теорія: {theory}, водіння: {lessons.Count - theory}, вільних слотів: {free})";
    }

    private void Generate()
    {
        GenerationResult? result = null;
        if (Run(() =>
            {
                using var db = Db.Create();
                var options = new ScheduleGenerationOptions { NotBefore = DateTime.Now };
                result = new ScheduleGenerator(db, options).GenerateWeek(WeekStart);
            }))
        {
            Load();
            System.Windows.MessageBox.Show(result!.Summary, "Формування розкладу");
        }
    }

    private void Add()
    {
        if (NewInstructor == null)
        {
            Status = "Оберіть викладача.";
            return;
        }
        if (!TryParseTime(NewTime, out var time)) return;
        var start = NewDate.Date.Add(time);
        var instructor = NewInstructor;
        var group = NewGroup;
        var kind = NewKind;
        var topic = NewTopic;

        if (Run(() =>
            {
                using var db = Db.Create();
                var service = new ScheduleService(db);
                if (kind == LessonKind.Theory)
                {
                    if (group == null) throw new DomainException("Оберіть групу для теоретичного заняття.");
                    service.AddTheoryLesson(group.Id, instructor.Id, start, topic);
                }
                else
                {
                    if (instructor.VehicleId == null)
                        throw new DomainException("За інструктором не закріплено автомобіль.");
                    service.AddDrivingSlot(instructor.Id, instructor.VehicleId.Value, start, DrivingLesson.DefaultDuration, topic);
                }
            }, "Заняття додано до розкладу."))
        {
            WeekStart = start;
            Load();
        }
    }

    private void Move()
    {
        if (Selected == null || !TryParseTime(MoveTime, out var time)) return;
        int id = Selected.Id;
        var newStart = MoveDate.Date.Add(time);
        if (Run(() =>
            {
                using var db = Db.Create();
                new ScheduleService(db).MoveLesson(id, newStart);
            }, "Заняття перенесено."))
        {
            Load();
        }
    }

    private void Cancel()
    {
        if (Selected == null || !Confirm($"Скасувати заняття {Selected.Day} {Selected.Time}?")) return;
        int id = Selected.Id;
        if (Run(() =>
            {
                using var db = Db.Create();
                new ScheduleService(db).CancelLesson(id);
            }, "Заняття скасовано."))
        {
            Load();
        }
    }

    private bool TryParseTime(string text, out TimeSpan time)
    {
        if (TimeSpan.TryParseExact(text?.Trim(), new[] { @"hh\:mm", @"h\:mm" }, CultureInfo.InvariantCulture, out time))
        {
            return true;
        }
        Status = "Час вказується у форматі ГГ:ХХ, наприклад 09:30.";
        return false;
    }
}
