using System.Globalization;
using System.Text;

namespace DrivingSchool.Core.Learning;

/// <summary>Формування звіту про успішність у форматі CSV (відкривається в Excel).</summary>
public class ReportService
{
    private readonly ProgressService _progress;

    public ReportService(ProgressService progress)
    {
        _progress = progress;
    }

    public string BuildProgressCsv(int? groupId)
    {
        var rows = _progress.GetGroupProgress(groupId);
        var culture = CultureInfo.GetCultureInfo("uk-UA");
        var sb = new StringBuilder();
        sb.AppendLine("Учень;Група;Відвідуваність теорії, %;Години водіння;Середній бал (теорія);Середній бал (водіння);Етап навчання");
        foreach (var p in rows)
        {
            sb.AppendLine(string.Join(";",
                Escape(p.StudentName),
                Escape(p.GroupName),
                p.TheoryAttendancePercent.ToString("0.0", culture),
                $"{p.DrivingHoursCompleted.ToString("0.0", culture)} з {p.DrivingHoursRequired}",
                p.AverageTheoryScore?.ToString("0.0", culture) ?? "-",
                p.AverageDrivingScore?.ToString("0.0", culture) ?? "-",
                Escape(p.StageText)));
        }
        return sb.ToString();
    }

    /// <summary>Зберігає звіт у файл у кодуванні UTF-8 з BOM, щоб Excel коректно показав кирилицю.</summary>
    public void ExportProgressCsv(int? groupId, string path)
    {
        File.WriteAllText(path, BuildProgressCsv(groupId), new UTF8Encoding(true));
    }

    private static string Escape(string value) =>
        value.Contains(';') || value.Contains('"') ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
}
