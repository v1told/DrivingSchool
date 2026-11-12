using DrivingSchool.Core.Models;

namespace DrivingSchool.Core.Scheduling;

/// <summary>
/// Дані, необхідні правилам для перевірки заняття-кандидата:
/// саме заняття, пов'язані ресурси та вже заплановані заняття.
/// </summary>
public class ScheduleContext
{
    public ScheduleContext(Lesson candidate, Instructor instructor, IReadOnlyList<Lesson> existing)
    {
        Candidate = candidate;
        Instructor = instructor;
        Existing = existing;
    }

    public Lesson Candidate { get; }

    public Instructor Instructor { get; }

    public Vehicle? Vehicle { get; init; }

    /// <summary>Заплановані заняття (без скасованих і без самого кандидата).</summary>
    public IReadOnlyList<Lesson> Existing { get; }

    public IEnumerable<Lesson> Overlapping =>
        Existing.Where(l => !ReferenceEquals(l, Candidate)
                            && (Candidate.Id == 0 || l.Id != Candidate.Id)
                            && l.Status != LessonStatus.Cancelled
                            && l.OverlapsWith(Candidate));
}
