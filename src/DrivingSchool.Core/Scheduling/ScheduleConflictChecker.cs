using DrivingSchool.Core.Models;

namespace DrivingSchool.Core.Scheduling;

/// <summary>
/// Перевіряє заняття-кандидата на відповідність усім правилам розкладу.
/// Набір правил передається ззовні, тому його легко розширювати
/// без зміни коду цього класу (принцип відкритості/закритості).
/// </summary>
public class ScheduleConflictChecker
{
    private readonly IReadOnlyList<IScheduleRule> _rules;

    public ScheduleConflictChecker(IEnumerable<IScheduleRule> rules)
    {
        _rules = rules.ToList();
    }

    /// <summary>Стандартний набір правил автошколи (робочий час 08:00–20:00).</summary>
    public static ScheduleConflictChecker CreateDefault() => new(new IScheduleRule[]
    {
        new WorkingHoursRule(new TimeSpan(8, 0, 0), new TimeSpan(20, 0, 0)),
        new InstructorQualificationRule(),
        new InstructorBusyRule(),
        new VehicleAvailabilityRule(),
        new VehicleBusyRule(),
        new GroupBusyRule(),
        new ClassroomBusyRule()
    });

    public IReadOnlyList<ScheduleConflict> FindConflicts(ScheduleContext ctx) =>
        _rules.SelectMany(rule => rule.Check(ctx)).ToList();

    public bool IsValid(ScheduleContext ctx) => FindConflicts(ctx).Count == 0;
}
