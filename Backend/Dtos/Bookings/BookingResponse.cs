namespace Spot4Hire.Backend.Dtos.Bookings;

public class BookingResponse
{
    public Guid Id { get; set; }

    public Guid UnitId { get; set; }

    public Guid UserId { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    // Derived from EndAt - StartAt (the entity exposes a TimeSpan Duration).
    public int DurationMinutes { get; set; }
}
