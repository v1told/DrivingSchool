using DrivingSchool.Core.Models;

namespace DrivingSchool.App.Infrastructure;

/// <summary>Пара «значення – підпис» для випадаючих списків.</summary>
public record Option<T>(T Value, string Name)
{
    public override string ToString() => Name;
}

/// <summary>Довідники з українськими назвами значень перелічень.</summary>
public static class Lookups
{
    public static List<Option<LicenseCategory>> Categories { get; } = new()
    {
        new(LicenseCategory.A, "A"), new(LicenseCategory.B, "B"), new(LicenseCategory.C, "C")
    };

    public static List<Option<Transmission>> Transmissions { get; } = new()
    {
        new(Transmission.Manual, "Механічна"), new(Transmission.Automatic, "Автоматична")
    };

    public static List<Option<LessonKind>> LessonKinds { get; } = new()
    {
        new(LessonKind.Theory, "Теоретичне"), new(LessonKind.Driving, "Практичне водіння")
    };

    public static List<Option<AttendanceStatus>> AttendanceStatuses { get; } = new()
    {
        new(AttendanceStatus.Present, "Присутній"),
        new(AttendanceStatus.Late, "Запізнився"),
        new(AttendanceStatus.Absent, "Відсутній"),
        new(AttendanceStatus.Excused, "Поважна причина")
    };

    public static List<Option<GradeType>> GradeTypes { get; } = new()
    {
        new(GradeType.TheoryTest, "Тест з теорії"),
        new(GradeType.DrivingSkill, "Навички водіння"),
        new(GradeType.InternalTheoryExam, "Внутрішній іспит (теорія)"),
        new(GradeType.InternalDrivingExam, "Внутрішній іспит (водіння)")
    };

    public static List<int> Scores { get; } = Enumerable.Range(1, 12).ToList();
}
