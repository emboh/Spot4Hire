namespace Spot4Hire.Backend.Data.Entities;

public class Booking
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public Guid UnitId { get; set; }

    public Unit Unit { get; set; } = null!;

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public TimeSpan Duration => EndAt - StartAt;
}
