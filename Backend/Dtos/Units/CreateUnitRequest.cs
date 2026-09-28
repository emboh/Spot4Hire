namespace Spot4Hire.Backend.Dtos.Units;

// VenueId is taken from the route (/venues/{venueId}/units), not the body.
// Validation lives in CreateUnitRequestValidator (FluentValidation).
public class CreateUnitRequest
{
    public string Name { get; set; } = string.Empty;

    public decimal? Price { get; set; }

    public string? Currency { get; set; } = "IDR";

    public string? Description { get; set; }

    // Minimum bookable duration in minutes (the entity stores this as a TimeSpan).
    public int MinBookingDurationMinutes { get; set; } = 60;
}
