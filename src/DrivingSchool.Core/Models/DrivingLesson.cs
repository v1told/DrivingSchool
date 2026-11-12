namespace DrivingSchool.Core.Models;

/// <summary>
/// Практичне заняття з водіння: індивідуальне, проводиться інструктором
/// на закріпленому автомобілі. Учень записується на нього через Booking.
/// </summary>
public class DrivingLesson : Lesson
{
    public const int DefaultDuration = 60;

    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public List<Booking> Bookings { get; set; } = new();

    public override LessonKind Kind => LessonKind.Driving;

    public override int Capacity => 1;

    public override string Location => Vehicle?.DisplayName ?? "автомобіль";

    /// <summary>Активний запис учня на це заняття (якщо є).</summary>
    public Booking? ActiveBooking => Bookings.FirstOrDefault(b => b.Status == BookingStatus.Active);

    public bool IsFree => ActiveBooking is null && Status == LessonStatus.Planned;
}
