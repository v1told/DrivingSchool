using DrivingSchool.Core.Common;

namespace DrivingSchool.Core.Models;

/// <summary>
/// Абстрактне заняття. Спільні для всіх занять дані (час, тривалість,
/// викладач, стан) винесені в базовий клас, а специфіка теоретичного
/// і практичного заняття реалізована в нащадках.
/// </summary>
public abstract class Lesson : IEntity
{
    public int Id { get; set; }

    public DateTime Start { get; set; }

    public int DurationMinutes { get; set; }

    public string Topic { get; set; } = string.Empty;

    public LessonStatus Status { get; set; } = LessonStatus.Planned;

    public int InstructorId { get; set; }
    public Instructor? Instructor { get; set; }

    public List<AttendanceRecord> Attendance { get; set; } = new();

    public DateTime End => Start.AddMinutes(DurationMinutes);

    public abstract LessonKind Kind { get; }

    /// <summary>Максимальна кількість учнів на занятті.</summary>
    public abstract int Capacity { get; }

    /// <summary>Місце проведення: аудиторія або автомобіль.</summary>
    public abstract string Location { get; }

    public string KindName => Kind == LessonKind.Theory ? "Теорія" : "Водіння";

    public string StatusName => Status switch
    {
        LessonStatus.Planned => "Заплановано",
        LessonStatus.Completed => "Проведено",
        _ => "Скасовано"
    };

    public string TimeRange => $"{Start:dd.MM.yyyy HH:mm}–{End:HH:mm}";

    /// <summary>Чи перетинається заняття в часі з іншим заняттям.</summary>
    public bool OverlapsWith(Lesson other) => Start < other.End && other.Start < End;
}
