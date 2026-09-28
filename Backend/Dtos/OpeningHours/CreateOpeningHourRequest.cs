namespace Spot4Hire.Backend.Dtos.OpeningHours;

// VenueId is taken from the route (/venues/{venueId}/opening-hours), not the body.
// Validation lives in CreateOpeningHourRequestValidator (FluentValidation).
public class CreateOpeningHourRequest
{
    public DayOfWeek Day { get; set; }

    public TimeOnly OpenTime { get; set; }

    public TimeOnly CloseTime { get; set; }
}
