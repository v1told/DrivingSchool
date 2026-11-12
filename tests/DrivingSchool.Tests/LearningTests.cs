using DrivingSchool.Core.Common;
using DrivingSchool.Core.Learning;
using DrivingSchool.Core.Models;
using DrivingSchool.Core.Scheduling;
using Xunit;

namespace DrivingSchool.Tests;

/// <summary>Тести модуля запису на заняття, обліку відвідувань і контролю прогресу.</summary>
public class LearningTests
{
    // Запис відбувається в неділю перед тестовим тижнем.
    private static readonly FixedClock BeforeWeek = new(TestDb.Monday.AddHours(-12));

    private static DrivingLesson Slot(TestDb t, int hour, int dayOffset = 0, bool automatic = false) =>
        new ScheduleService(t.Context).AddDrivingSlot(
            automatic ? t.Driver2.Id : t.Driver1.Id,
            automatic ? t.AutoCar.Id : t.ManualCar.Id,
            TestDb.Monday.AddDays(dayOffset).AddHours(hour));

    [Fact]
    public void Book_SucceedsForMatchingSlot()
    {
        using var t = new TestDb();
        var slot = Slot(t, 10);

        var booking = new BookingService(t.Context, clock: BeforeWeek).Book(t.Anna.Id, slot.Id);

        Assert.Equal(BookingStatus.Active, booking.Status);
        Assert.Equal(t.Anna.Id, booking.StudentId);
    }

    [Fact]
    public void Book_FailsWhenSlotTaken()
    {
        using var t = new TestDb();
        var slot = Slot(t, 10, automatic: true);
        var service = new BookingService(t.Context, clock: BeforeWeek);
        service.Book(t.Ivan.Id, slot.Id);

        t.Context.Students.Add(new Student { LastName = "Мороз", FirstName = "Віка", Phone = "+380501112299", BirthDate = new DateTime(2004, 1, 1), PreferredTransmission = Transmission.Automatic });
        t.Context.SaveChanges();
        int otherId = t.Context.Students.Single(s => s.LastName == "Мороз").Id;

        var ex = Assert.Throws<DomainException>(() => service.Book(otherId, slot.Id));
        Assert.Contains("інший учень", ex.Message);
    }

    [Fact]
    public void Book_FailsOnTransmissionMismatch()
    {
        using var t = new TestDb();
        var autoSlot = Slot(t, 10, automatic: true);

        Assert.Throws<DomainException>(() =>
            new BookingService(t.Context, clock: BeforeWeek).Book(t.Anna.Id, autoSlot.Id));
    }

    [Fact]
    public void Book_FailsWhenOverlapsGroupTheory()
    {
        using var t = new TestDb();
        new ScheduleService(t.Context).AddTheoryLesson(t.Group.Id, t.Theory.Id, TestDb.Monday.AddHours(16), "Тема");
        var slot = Slot(t, 17);

        var ex = Assert.Throws<DomainException>(() =>
            new BookingService(t.Context, clock: BeforeWeek).Book(t.Anna.Id, slot.Id));
        Assert.Contains("інше заняття", ex.Message);
    }

    [Fact]
    public void Book_FailsWhenDailyLimitExceeded()
    {
        using var t = new TestDb();
        var service = new BookingService(t.Context, new LearningPolicy { MaxDrivingLessonsPerDay = 2 }, BeforeWeek);
        service.Book(t.Anna.Id, Slot(t, 8).Id);
        service.Book(t.Anna.Id, Slot(t, 9).Id);

        Assert.Throws<DomainException>(() => service.Book(t.Anna.Id, Slot(t, 11).Id));
    }

    [Fact]
    public void GetAvailableSlots_ReturnsOnlySuitableFreeSlots()
    {
        using var t = new TestDb();
        Slot(t, 8);
        Slot(t, 9, automatic: true);
        var taken = Slot(t, 10);
        var service = new BookingService(t.Context, clock: BeforeWeek);
        service.Book(t.Anna.Id, taken.Id);

        var slots = service.GetAvailableSlots(t.Anna.Id, TestDb.Monday, TestDb.Monday.AddDays(7));

        Assert.Single(slots);
        Assert.Equal(TestDb.Monday.AddHours(8), slots[0].Start);
    }

    [Fact]
    public void Cancel_FailsLessThan24HoursBefore()
    {
        using var t = new TestDb();
        var slot = Slot(t, 10);
        var booking = new BookingService(t.Context, clock: BeforeWeek).Book(t.Anna.Id, slot.Id);
        var lateClock = new FixedClock(TestDb.Monday.AddHours(8));

        Assert.Throws<DomainException>(() => new BookingService(t.Context, clock: lateClock).Cancel(booking.Id));
    }

    [Fact]
    public void Cancel_FreesSlot()
    {
        using var t = new TestDb();
        var slot = Slot(t, 10, dayOffset: 2);
        var service = new BookingService(t.Context, clock: BeforeWeek);
        var booking = service.Book(t.Anna.Id, slot.Id);

        service.Cancel(booking.Id);

        var free = service.GetAvailableSlots(t.Anna.Id, TestDb.Monday, TestDb.Monday.AddDays(7));
        Assert.Contains(free, s => s.Id == slot.Id);
    }

    [Fact]
    public void SaveJournal_FailsForFutureLesson()
    {
        using var t = new TestDb();
        var lesson = new ScheduleService(t.Context).AddTheoryLesson(t.Group.Id, t.Theory.Id, TestDb.Monday.AddHours(18), "Тема");
        var service = new AttendanceService(t.Context, BeforeWeek);
        var journal = service.GetJournal(lesson.Id);

        Assert.Throws<DomainException>(() => service.SaveJournal(lesson.Id, journal));
    }

    [Fact]
    public void Progress_CountsAttendanceAndDrivingHours()
    {
        using var t = new TestDb();
        var schedule = new ScheduleService(t.Context);
        var l1 = schedule.AddTheoryLesson(t.Group.Id, t.Theory.Id, TestDb.Monday.AddHours(18), "Тема 1");
        var l2 = schedule.AddTheoryLesson(t.Group.Id, t.Theory.Id, TestDb.Monday.AddDays(2).AddHours(18), "Тема 2");
        var drive = Slot(t, 10);
        new BookingService(t.Context, clock: BeforeWeek).Book(t.Anna.Id, drive.Id);

        var afterWeek = new FixedClock(TestDb.Monday.AddDays(6));
        var attendance = new AttendanceService(t.Context, afterWeek);
        Mark(attendance, l1.Id, t.Anna.Id, AttendanceStatus.Present);
        Mark(attendance, l2.Id, t.Anna.Id, AttendanceStatus.Absent);
        Mark(attendance, drive.Id, t.Anna.Id, AttendanceStatus.Present);

        var progress = new ProgressService(t.Context).GetProgress(t.Anna.Id);

        Assert.Equal(2, progress.TheoryLessonsHeld);
        Assert.Equal(1, progress.TheoryLessonsAttended);
        Assert.Equal(50.0, progress.TheoryAttendancePercent);
        Assert.Equal(1.0, progress.DrivingHoursCompleted);
        Assert.Equal(25.0, progress.DrivingPercent);
        Assert.False(progress.AdmittedToTheoryExam);
    }

    [Fact]
    public void Progress_AdmitsToTheoryExamWhenCriteriaMet()
    {
        using var t = new TestDb();
        var lesson = new ScheduleService(t.Context).AddTheoryLesson(t.Group.Id, t.Theory.Id, TestDb.Monday.AddHours(18), "Тема");
        var attendance = new AttendanceService(t.Context, new FixedClock(TestDb.Monday.AddDays(1)));
        Mark(attendance, lesson.Id, t.Anna.Id, AttendanceStatus.Present);
        attendance.AddGrade(t.Anna.Id, GradeType.TheoryTest, 9);
        attendance.AddGrade(t.Anna.Id, GradeType.InternalTheoryExam, 10);

        var progress = new ProgressService(t.Context).GetProgress(t.Anna.Id);

        Assert.True(progress.AdmittedToTheoryExam);
        Assert.True(progress.TheoryExamPassed);
        Assert.False(progress.AdmittedToDrivingExam); // години водіння ще не відпрацьовані
    }

    [Fact]
    public void SaveJournal_FailsBeforeLessonEnds()
    {
        using var t = new TestDb();
        var lesson = new ScheduleService(t.Context).AddTheoryLesson(t.Group.Id, t.Theory.Id, TestDb.Monday.AddHours(18), "Тема");
        // 18:30: заняття вже почалося, але ще не завершилося (до 19:30).
        var service = new AttendanceService(t.Context, new FixedClock(TestDb.Monday.AddHours(18.5)));

        Assert.Empty(service.GetLessonsForJournal(TestDb.Monday, TestDb.Monday.AddDays(7)));
        Assert.Throws<DomainException>(() => service.SaveJournal(lesson.Id, service.GetJournal(lesson.Id)));
    }

    [Fact]
    public void Book_FailsWhenProgramHoursAlreadyCompleted()
    {
        using var t = new TestDb();
        SetRequiredHours(t, 1);
        var monday = Slot(t, 10);
        new BookingService(t.Context, clock: BeforeWeek).Book(t.Anna.Id, monday.Id);
        var tuesday = new FixedClock(TestDb.Monday.AddDays(1));
        Mark(new AttendanceService(t.Context, tuesday), monday.Id, t.Anna.Id, AttendanceStatus.Present);
        var wednesday = Slot(t, 10, dayOffset: 2);

        var service = new BookingService(t.Context, clock: tuesday);

        Assert.Empty(service.GetAvailableSlots(t.Anna.Id, TestDb.Monday, TestDb.Monday.AddDays(7)));
        var ex = Assert.Throws<DomainException>(() => service.Book(t.Anna.Id, wednesday.Id));
        Assert.Contains("відпрацьовані", ex.Message);
    }

    [Fact]
    public void Book_AllowsReplacementOfMissedLesson()
    {
        using var t = new TestDb();
        SetRequiredHours(t, 1);
        var monday = Slot(t, 10);
        new BookingService(t.Context, clock: BeforeWeek).Book(t.Anna.Id, monday.Id);
        var tuesday = new FixedClock(TestDb.Monday.AddDays(1));
        Mark(new AttendanceService(t.Context, tuesday), monday.Id, t.Anna.Id, AttendanceStatus.Absent);
        var wednesday = Slot(t, 10, dayOffset: 2);

        // Пропущене заняття не зараховується, тому година лишається доступною.
        var booking = new BookingService(t.Context, clock: tuesday).Book(t.Anna.Id, wednesday.Id);

        Assert.Equal(BookingStatus.Active, booking.Status);
    }

    [Fact]
    public void GetAvailableSlots_ExcludesDayAtDailyLimit()
    {
        using var t = new TestDb();
        var service = new BookingService(t.Context, new LearningPolicy { MaxDrivingLessonsPerDay = 2 }, BeforeWeek);
        service.Book(t.Anna.Id, Slot(t, 8).Id);
        service.Book(t.Anna.Id, Slot(t, 9).Id);
        Slot(t, 11);
        var tuesdaySlot = Slot(t, 8, dayOffset: 1);

        var slots = service.GetAvailableSlots(t.Anna.Id, TestDb.Monday, TestDb.Monday.AddDays(7));

        Assert.Single(slots);
        Assert.Equal(tuesdaySlot.Id, slots[0].Id);
    }

    [Fact]
    public void GetAvailableSlots_EmptyWhenWeeklyLimitReached()
    {
        using var t = new TestDb();
        var service = new BookingService(t.Context, new LearningPolicy { MaxDrivingLessonsPerWeek = 2 }, BeforeWeek);
        service.Book(t.Anna.Id, Slot(t, 8).Id);
        service.Book(t.Anna.Id, Slot(t, 8, dayOffset: 1).Id);
        Slot(t, 8, dayOffset: 2);

        Assert.Empty(service.GetAvailableSlots(t.Anna.Id, TestDb.Monday, TestDb.Monday.AddDays(7)));
    }

    [Fact]
    public void GetAvailableSlots_EmptyWhenNoHoursLeft()
    {
        using var t = new TestDb();
        SetRequiredHours(t, 1);
        var service = new BookingService(t.Context, clock: BeforeWeek);
        service.Book(t.Anna.Id, Slot(t, 8).Id);
        Slot(t, 8, dayOffset: 1);

        Assert.Empty(service.GetAvailableSlots(t.Anna.Id, TestDb.Monday, TestDb.Monday.AddDays(7)));
    }

    [Fact]
    public void Progress_IgnoresDrivingLessonWithoutJournal()
    {
        using var t = new TestDb();
        var drive = Slot(t, 10);
        new BookingService(t.Context, clock: BeforeWeek).Book(t.Anna.Id, drive.Id);

        // Заняття минуло, але журнал не заповнено: години не зараховуються.
        var progress = new ProgressService(t.Context).GetProgress(t.Anna.Id);

        Assert.Equal(0.0, progress.DrivingHoursCompleted);
    }

    private static void SetRequiredHours(TestDb t, int hours)
    {
        t.Anna.RequiredDrivingHours = hours;
        t.Context.SaveChanges();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void AddGrade_RejectsScoreOutOfRange(int score)
    {
        using var t = new TestDb();
        var service = new AttendanceService(t.Context, BeforeWeek);

        Assert.Throws<DomainException>(() => service.AddGrade(t.Anna.Id, GradeType.TheoryTest, score));
    }

    private static void Mark(AttendanceService service, int lessonId, int studentId, AttendanceStatus status)
    {
        var journal = service.GetJournal(lessonId);
        foreach (var e in journal)
        {
            e.Status = e.StudentId == studentId ? status : AttendanceStatus.Absent;
        }
        service.SaveJournal(lessonId, journal);
    }
}
