using System.Collections.ObjectModel;
using System.IO;
using DrivingSchool.App.Infrastructure;
using DrivingSchool.Core.Learning;
using DrivingSchool.Core.Models;
using DrivingSchool.Core.Scheduling;
using Microsoft.Win32;

namespace DrivingSchool.App.ViewModels;

/// <summary>
/// Контроль успішності та навчального прогресу: зведена таблиця по групі,
/// детальна картка учня з оцінками та експорт звіту в CSV.
/// </summary>
public class ProgressViewModel : ViewModelBase
{
    private StudyGroup? _group;
    private StudentProgress? _selected;

    public ProgressViewModel()
    {
        RefreshCommand = new RelayCommand(Load);
        ExportCommand = new RelayCommand(Export, () => Rows.Count > 0);
        AllGroupsCommand = new RelayCommand(() => Group = null);
        LoadGroups();
        Load();
    }

    public ObservableCollection<StudyGroup> Groups { get; } = new();
    public ObservableCollection<StudentProgress> Rows { get; } = new();
    public ObservableCollection<Grade> SelectedGrades { get; } = new();

    public RelayCommand RefreshCommand { get; }
    public RelayCommand ExportCommand { get; }
    public RelayCommand AllGroupsCommand { get; }

    /// <summary>Обрана група; null означає «усі учні».</summary>
    public StudyGroup? Group
    {
        get => _group;
        set { if (SetProperty(ref _group, value)) Load(); }
    }

    public StudentProgress? Selected
    {
        get => _selected;
        set
        {
            if (SetProperty(ref _selected, value)) LoadGrades();
        }
    }

    public void LoadGroups()
    {
        using var db = Db.Create();
        Groups.Clear();
        foreach (var g in new ResourceService(db).GetGroups()) Groups.Add(g);
    }

    public void Load()
    {
        using var db = Db.Create();
        Rows.Clear();
        foreach (var p in new ProgressService(db).GetGroupProgress(Group?.Id)) Rows.Add(p);
        int admitted = Rows.Count(r => r.AdmittedToTheoryExam);
        Status = Rows.Count == 0
            ? "Немає учнів."
            : $"Учнів: {Rows.Count}; середня відвідуваність теорії: {Rows.Average(r => r.TheoryAttendancePercent):0.0} %; допущено до іспиту з теорії: {admitted}";
    }

    private void LoadGrades()
    {
        SelectedGrades.Clear();
        if (Selected == null) return;
        using var db = Db.Create();
        foreach (var g in new AttendanceService(db).GetGrades(Selected.StudentId)) SelectedGrades.Add(g);
    }

    private void Export()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"Успішність_{Group?.Name ?? "усі"}_{DateTime.Today:yyyyMMdd}.csv"
        };
        if (dialog.ShowDialog() != true) return;
        string path = dialog.FileName;
        int? groupId = Group?.Id;
        Run(() =>
        {
            using var db = Db.Create();
            new ReportService(new ProgressService(db)).ExportProgressCsv(groupId, path);
        }, $"Звіт збережено: {Path.GetFileName(path)}");
    }
}
