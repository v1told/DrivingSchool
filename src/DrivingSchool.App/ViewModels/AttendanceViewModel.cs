using System.Collections.ObjectModel;
using DrivingSchool.App.Infrastructure;
using DrivingSchool.Core.Common;
using DrivingSchool.Core.Learning;
using DrivingSchool.Core.Models;

namespace DrivingSchool.App.ViewModels;

/// <summary>
/// Журнал відвідувань і оцінювання: вибір проведеного заняття,
/// відмітки присутності та виставлення оцінок учням.
/// </summary>
public class AttendanceViewModel : ViewModelBase
{
    private DateTime _weekStart = DateHelper.StartOfWeek(DateTime.Today);
    private LessonRow? _selectedLesson;
    private AttendanceEntry? _selectedEntry;
    private GradeType _gradeType = GradeType.TheoryTest;
    private int _score = 10;
    private string _comment = string.Empty;

    public AttendanceViewModel()
    {
        PrevWeekCommand = new RelayCommand(() => WeekStart = WeekStart.AddDays(-7));
        NextWeekCommand = new RelayCommand(() => WeekStart = WeekStart.AddDays(7));
        SaveCommand = new RelayCommand(SaveJournal, () => SelectedLesson != null && Journal.Count > 0);
        AllPresentCommand = new RelayCommand(MarkAllPresent, () => Journal.Count > 0);
        GradeCommand = new RelayCommand(AddGrade, () => SelectedEntry != null);
        Load();
    }

    public ObservableCollection<LessonRow> Lessons { get; } = new();
    public ObservableCollection<AttendanceEntry> Journal { get; } = new();

    public RelayCommand PrevWeekCommand { get; }
    public RelayCommand NextWeekCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand AllPresentCommand { get; }
    public RelayCommand GradeCommand { get; }

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

    public LessonRow? SelectedLesson
    {
        get => _selectedLesson;
        set
        {
            if (SetProperty(ref _selectedLesson, value)) LoadJournal();
        }
    }

    public AttendanceEntry? SelectedEntry { get => _selectedEntry; set => SetProperty(ref _selectedEntry, value); }

    public GradeType GradeType { get => _gradeType; set => SetProperty(ref _gradeType, value); }
    public int Score { get => _score; set => SetProperty(ref _score, value); }
    public string Comment { get => _comment; set => SetProperty(ref _comment, value); }

    public void Load()
    {
        using var db = Db.Create();
        var lessons = new AttendanceService(db).GetLessonsForJournal(WeekStart, WeekStart.AddDays(7));
        Lessons.Clear();
        foreach (var l in lessons) Lessons.Add(new LessonRow(l));
        Journal.Clear();
        Status = $"Занять для журналу: {Lessons.Count} (проведено: {lessons.Count(l => l.Status == LessonStatus.Completed)})";
    }

    private void LoadJournal()
    {
        Journal.Clear();
        if (SelectedLesson == null) return;
        using var db = Db.Create();
        foreach (var e in new AttendanceService(db).GetJournal(SelectedLesson.Id)) Journal.Add(e);
        GradeType = SelectedLesson.Kind == "Теорія" ? GradeType.TheoryTest : GradeType.DrivingSkill;
    }

    private void MarkAllPresent()
    {
        var copy = Journal.ToList();
        foreach (var e in copy) e.Status = AttendanceStatus.Present;
        Journal.Clear();
        foreach (var e in copy) Journal.Add(e);
    }

    private void SaveJournal()
    {
        if (SelectedLesson == null) return;
        int lessonId = SelectedLesson.Id;
        var entries = Journal.ToList();
        if (Run(() =>
            {
                using var db = Db.Create();
                new AttendanceService(db).SaveJournal(lessonId, entries);
            }, "Відмітки відвідування збережено."))
        {
            var keep = lessonId;
            Load();
            SelectedLesson = Lessons.FirstOrDefault(l => l.Id == keep);
        }
    }

    private void AddGrade()
    {
        if (SelectedEntry == null) return;
        int studentId = SelectedEntry.StudentId;
        int? lessonId = SelectedLesson?.Id;
        var type = GradeType;
        int score = Score;
        string? comment = string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim();
        string name = SelectedEntry.StudentName;
        if (Run(() =>
            {
                using var db = Db.Create();
                new AttendanceService(db).AddGrade(studentId, type, score, lessonId, comment);
            }, $"Оцінку {score} виставлено: {name}."))
        {
            Comment = string.Empty;
        }
    }
}
