using DrivingSchool.Core.Common;
using DrivingSchool.Core.Models;

namespace DrivingSchool.App.ViewModels;

/// <summary>Рядок таблиці розкладу: дані заняття, підготовлені для відображення.</summary>
public class LessonRow
{
    public LessonRow(Lesson lesson)
    {
        Id = lesson.Id;
        Start = lesson.Start;
        Day = $"{DateHelper.DayName(lesson.Start.DayOfWeek)}, {lesson.Start:dd.MM}";
        Time = $"{lesson.Start:HH:mm}–{lesson.End:HH:mm}";
        Kind = lesson.KindName;
        Topic = lesson.Topic;
        Instructor = lesson.Instructor?.ShortName ?? string.Empty;
        Status = lesson.StatusName;

        switch (lesson)
        {
            case TheoryLesson t:
                Participants = t.Group?.Name ?? string.Empty;
                Place = t.Classroom;
                break;
            case DrivingLesson d:
                Participants = d.ActiveBooking?.Student?.ShortName ?? "вільно";
                Place = d.Vehicle?.DisplayName ?? string.Empty;
                break;
        }
    }

    public int Id { get; }
    public DateTime Start { get; }
    public string Day { get; }
    public string Time { get; }
    public string Kind { get; }
    public string Topic { get; }
    public string Instructor { get; }
    public string Participants { get; } = string.Empty;
    public string Place { get; } = string.Empty;
    public string Status { get; }
}
