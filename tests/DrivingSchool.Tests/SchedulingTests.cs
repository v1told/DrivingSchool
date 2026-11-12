using DrivingSchool.Core.Common;
using DrivingSchool.Core.Models;
using DrivingSchool.Core.Scheduling;
using Xunit;

namespace DrivingSchool.Tests;

/// <summary>Тести модуля обліку ресурсів і формування розкладу.</summary>
public class SchedulingTests
{
    private static ScheduleContext Ctx(Lesson candidate, Instructor instructor, params Lesson[] existing) =>
        new(candidate, instructor, existing.ToList());

    [Fact]
    public void Checker_DetectsInstructorOverlap()
    {
        var instructor = new Instructor { Id = 1, LastName = "Бондар", FirstName = "Сергій", TeachesDriving = true };
        var vehicle = new Vehicle { Id = 1, IsAvailable = true };
        var existing = new DrivingLesson { Id = 10, InstructorId = 1, VehicleId = 1, Start = TestDb.Monday.AddHours(10), DurationMinutes = 60 };
        var candidate = new DrivingLesson { InstructorId = 1, VehicleId = 1, Start = TestDb.Monday.AddHours(10.5), DurationMinutes = 60 };

        var conflicts = ScheduleConflictChecker.CreateDefault()
            .FindConflicts(new ScheduleContext(candidate, instructor, new List<Lesson> { existing }) { Vehicle = vehicle });

        Assert.Contains(conflicts, c => c.Type == ConflictType.InstructorBusy);
        Assert.Contains(conflicts, c => c.Type == ConflictType.VehicleBusy);
    }

    [Fact]
    public void Checker_AllowsAdjacentLessons()
    {
        var instructor = new Instructor { Id = 1, LastName = "Бондар", FirstName = "Сергій", TeachesDriving = true };
        var existing = new DrivingLesson { Id = 10, InstructorId = 1, VehicleId = 1, Start = TestDb.Monday.AddHours(10), DurationMinutes = 60 };
        var candidate = new DrivingLesson { InstructorId = 1, VehicleId = 1, Start = TestDb.Monday.AddHours(11), DurationMinutes = 60 };

        var ctx = new ScheduleContext(candidate, instructor, new List<Lesson> { existing }) { Vehicle = new Vehicle { IsAvailable = true } };

        Assert.True(ScheduleConflictChecker.CreateDefault().IsValid(ctx));
    }

    [Theory]
    [InlineData(7, 0)]    // до відкриття
    [InlineData(19, 30)]  // закінчується після 20:00
    public void Checker_RejectsOutsideWorkingHours(int hour, int minute)
    {
        var instructor = new Instructor { Id = 1, LastName = "Коваль", FirstName = "Олена", TeachesTheory = true };
        var lesson = new TheoryLesson { InstructorId = 1, GroupId = 1, Start = TestDb.Monday.Add(new TimeSpan(hour, minute, 0)), DurationMinutes = 90 };

        var conflicts = ScheduleConflictChecker.CreateDefault().FindConflicts(Ctx(lesson, instructor));

        Assert.Contains(conflicts, c => c.Type == ConflictType.OutsideWorkingHours);
    }

    [Fact]
    public void Checker_RejectsTheoryForDrivingOnlyInstructor()
    {
        var instructor = new Instructor { Id = 2, LastName = "Бондар", FirstName = "Сергій", TeachesTheory = false, TeachesDriving = true };
        var lesson = new TheoryLesson { InstructorId = 2, GroupId = 1, Start = TestDb.Monday.AddHours(18), DurationMinutes = 90 };

        var conflicts = ScheduleConflictChecker.CreateDefault().FindConflicts(Ctx(lesson, instructor));

        Assert.Single(conflicts);
        Assert.Equal(ConflictType.InstructorNotQualified, conflicts[0].Type);
    }

    [Fact]
    public void Service_AddTheory_ThrowsOnGroupConflict()
    {
        using var t = new TestDb();
        var service = new ScheduleService(t.Context);
        service.AddTheoryLesson(t.Group.Id, t.Theory.Id, TestDb.Monday.AddHours(18), "Тема 1");

        var ex = Assert.Throws<ScheduleConflictException>(() =>
            service.AddTheoryLesson(t.Group.Id, t.Theory.Id, TestDb.Monday.AddHours(18.5), "Тема 2"));

        Assert.Contains(ex.Conflicts, c => c.Type == ConflictType.GroupBusy);
    }

    [Fact]
    public void Service_MoveLesson_KeepsOldTimeOnConflict()
    {
        using var t = new TestDb();
        var service = new ScheduleService(t.Context);
        var first = service.AddDrivingSlot(t.Driver1.Id, t.ManualCar.Id, TestDb.Monday.AddHours(9));
        var second = service.AddDrivingSlot(t.Driver1.Id, t.ManualCar.Id, TestDb.Monday.AddHours(11));

        Assert.Throws<ScheduleConflictException>(() => service.MoveLesson(second.Id, TestDb.Monday.AddHours(9)));

        using var check = t.CreateContext();
        Assert.Equal(TestDb.Monday.AddHours(11), check.Lessons.Single(l => l.Id == second.Id).Start);
    }

    [Fact]
    public void Generator_CreatesRequiredTheoryWithoutConflicts()
    {
        using var t = new TestDb();
        var result = new ScheduleGenerator(t.Context).GenerateWeek(TestDb.Monday);

        Assert.Equal(3, result.TheoryCreated);
        var theory = t.Context.TheoryLessons.ToList();
        Assert.Equal(3, theory.Select(l => l.Start.Date).Distinct().Count()); // одне заняття групи на день
    }

    [Fact]
    public void Generator_CreatesSlotsAccordingToDemand()
    {
        using var t = new TestDb();
        var options = new ScheduleGenerationOptions { MaxDrivingLessonsPerStudentPerWeek = 3 };

        var result = new ScheduleGenerator(t.Context, options).GenerateWeek(TestDb.Monday);

        // Анна (механіка) і Іван (автомат): по 3 слоти на кожен тип автомобіля.
        Assert.Equal(6, result.DrivingSlotsCreated);
        var slots = t.Context.DrivingLessons.ToList();
        Assert.Equal(3, slots.Count(s => s.VehicleId == t.ManualCar.Id));
        Assert.Equal(3, slots.Count(s => s.VehicleId == t.AutoCar.Id));
    }

    [Fact]
    public void Generator_IsIdempotentForSameWeek()
    {
        using var t = new TestDb();
        new ScheduleGenerator(t.Context).GenerateWeek(TestDb.Monday);

        var second = new ScheduleGenerator(t.Context).GenerateWeek(TestDb.Monday.AddDays(2));

        Assert.Equal(0, second.TheoryCreated);
        Assert.Equal(0, second.DrivingSlotsCreated);
    }

    [Theory]
    [InlineData("ВК12АА")]
    [InlineData("12345678")]
    public void Resources_RejectInvalidPlate(string plate)
    {
        using var t = new TestDb();
        var service = new ResourceService(t.Context);

        Assert.Throws<DomainException>(() => service.SaveVehicle(new Vehicle { Model = "Renault Logan", PlateNumber = plate }));
    }

    [Fact]
    public void Resources_RejectStudentFromWrongCategoryGroup()
    {
        using var t = new TestDb();
        var service = new ResourceService(t.Context);
        var student = new Student
        {
            LastName = "Мороз", FirstName = "Віка", Phone = "+380501234567",
            BirthDate = new DateTime(2004, 1, 1), Category = LicenseCategory.A, GroupId = t.Group.Id
        };

        Assert.Throws<DomainException>(() => service.SaveStudent(student));
    }
}
