namespace DrivingSchool.Core.Scheduling;

/// <summary>Вид порушення обмежень розкладу.</summary>
public enum ConflictType
{
    OutsideWorkingHours,
    InstructorBusy,
    InstructorNotQualified,
    VehicleBusy,
    VehicleUnavailable,
    GroupBusy,
    ClassroomBusy
}

/// <summary>Опис одного конфлікту, знайденого під час перевірки заняття.</summary>
public record ScheduleConflict(ConflictType Type, string Message)
{
    public override string ToString() => Message;
}

/// <summary>Виняток, що містить перелік конфліктів розкладу.</summary>
public class ScheduleConflictException : DrivingSchool.Core.Common.DomainException
{
    public ScheduleConflictException(IReadOnlyList<ScheduleConflict> conflicts)
        : base("Заняття неможливо додати:\n" + string.Join("\n", conflicts.Select(c => "• " + c.Message)))
    {
        Conflicts = conflicts;
    }

    public IReadOnlyList<ScheduleConflict> Conflicts { get; }
}
