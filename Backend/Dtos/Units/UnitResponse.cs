namespace Spot4Hire.Backend.Dtos.Units;

public class UnitResponse
{
    public Guid Id { get; set; }

    public Guid VenueId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal? Price { get; set; }

    public string? Currency { get; set; }

    public string? Description { get; set; }

    public int MinBookingDurationMinutes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
