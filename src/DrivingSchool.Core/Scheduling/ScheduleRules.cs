using DrivingSchool.Core.Models;

namespace DrivingSchool.Core.Scheduling;

/// <summary>
/// Правило розкладу (патерн «Стратегія»). Кожне правило перевіряє одне
/// обмеження і повертає знайдені конфлікти.
/// </summary>
public interface IScheduleRule
{
    IEnumerable<ScheduleConflict> Check(ScheduleContext ctx);
}

/// <summary>Заняття мають проходити в робочий час автошколи.</summary>
public class WorkingHoursRule : IScheduleRule
{
    private readonly TimeSpan _open;
    private readonly TimeSpan _close;

    public WorkingHoursRule(TimeSpan open, TimeSpan close)
    {
        _open = open;
        _close = close;
    }

    public IEnumerable<ScheduleConflict> Check(ScheduleContext ctx)
    {
        var c = ctx.Candidate;
        if (c.DurationMinutes <= 0)
        {
            yield return new ScheduleConflict(ConflictType.OutsideWorkingHours,
                "Тривалість заняття має бути більшою за нуль.");
            yield break;
        }
        if (c.Start.DayOfWeek == DayOfWeek.Sunday)
        {
            yield return new ScheduleConflict(ConflictType.OutsideWorkingHours,
                "У неділю автошкола не працює.");
        }
        if (c.Start.TimeOfDay < _open || c.End.TimeOfDay > _close || c.End.Date != c.Start.Date)
        {
            yield return new ScheduleConflict(ConflictType.OutsideWorkingHours,
                $"Заняття виходить за межі робочого часу ({_open:hh\\:mm}–{_close:hh\\:mm}).");
        }
    }
}

/// <summary>Викладач не може проводити два заняття одночасно.</summary>
public class InstructorBusyRule : IScheduleRule
{
    public IEnumerable<ScheduleConflict> Check(ScheduleContext ctx) =>
        ctx.Overlapping
            .Where(l => l.InstructorId == ctx.Candidate.InstructorId)
            .Select(l => new ScheduleConflict(ConflictType.InstructorBusy,
                $"{ctx.Instructor.ShortName} у цей час уже має заняття ({l.KindName}, {l.TimeRange})."));
}

/// <summary>Викладач повинен мати право проводити заняття відповідного виду.</summary>
public class InstructorQualificationRule : IScheduleRule
{
    public IEnumerable<ScheduleConflict> Check(ScheduleContext ctx)
    {
        if (ctx.Candidate.Kind == LessonKind.Theory && !ctx.Instructor.TeachesTheory)
        {
            yield return new ScheduleConflict(ConflictType.InstructorNotQualified,
                $"{ctx.Instructor.ShortName} не викладає теоретичний курс.");
        }
        if (ctx.Candidate.Kind == LessonKind.Driving && !ctx.Instructor.TeachesDriving)
        {
            yield return new ScheduleConflict(ConflictType.InstructorNotQualified,
                $"{ctx.Instructor.ShortName} не проводить практичне водіння.");
        }
    }
}

/// <summary>Один автомобіль не може бути задіяний у двох заняттях одночасно.</summary>
public class VehicleBusyRule : IScheduleRule
{
    public IEnumerable<ScheduleConflict> Check(ScheduleContext ctx)
    {
        if (ctx.Candidate is not DrivingLesson driving)
        {
            return Enumerable.Empty<ScheduleConflict>();
        }
        return ctx.Overlapping
            .OfType<DrivingLesson>()
            .Where(l => l.VehicleId == driving.VehicleId)
            .Select(l => new ScheduleConflict(ConflictType.VehicleBusy,
                $"Автомобіль уже зайнятий у цей час ({l.TimeRange})."));
    }
}

/// <summary>Автомобіль має бути справним і відповідати категорії навчання.</summary>
public class VehicleAvailabilityRule : IScheduleRule
{
    public IEnumerable<ScheduleConflict> Check(ScheduleContext ctx)
    {
        if (ctx.Candidate is not DrivingLesson)
        {
            yield break;
        }
        if (ctx.Vehicle is null)
        {
            yield return new ScheduleConflict(ConflictType.VehicleUnavailable,
                "Для практичного заняття не вказано автомобіль.");
        }
        else if (!ctx.Vehicle.IsAvailable)
        {
            yield return new ScheduleConflict(ConflictType.VehicleUnavailable,
                $"Автомобіль {ctx.Vehicle.DisplayName} недоступний (ремонт або ТО).");
        }
    }
}

/// <summary>Група не може мати два теоретичні заняття одночасно.</summary>
public class GroupBusyRule : IScheduleRule
{
    public IEnumerable<ScheduleConflict> Check(ScheduleContext ctx)
    {
        if (ctx.Candidate is not TheoryLesson theory)
        {
            return Enumerable.Empty<ScheduleConflict>();
        }
        return ctx.Overlapping
            .OfType<TheoryLesson>()
            .Where(l => l.GroupId == theory.GroupId)
            .Select(l => new ScheduleConflict(ConflictType.GroupBusy,
                $"Група вже має заняття в цей час ({l.TimeRange})."));
    }
}

/// <summary>В одній аудиторії одночасно може бути лише одне заняття.</summary>
public class ClassroomBusyRule : IScheduleRule
{
    public IEnumerable<ScheduleConflict> Check(ScheduleContext ctx)
    {
        if (ctx.Candidate is not TheoryLesson theory || string.IsNullOrWhiteSpace(theory.Classroom))
        {
            return Enumerable.Empty<ScheduleConflict>();
        }
        return ctx.Overlapping
            .OfType<TheoryLesson>()
            .Where(l => string.Equals(l.Classroom, theory.Classroom, StringComparison.OrdinalIgnoreCase))
            .Select(l => new ScheduleConflict(ConflictType.ClassroomBusy,
                $"Аудиторія «{theory.Classroom}» зайнята ({l.TimeRange})."));
    }
}
