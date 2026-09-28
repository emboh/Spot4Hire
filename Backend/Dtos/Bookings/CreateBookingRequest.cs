namespace Spot4Hire.Backend.Dtos.Bookings;

// UnitId is taken from the route (/units/{unitId}/bookings), not the body.
// Validation lives in CreateBookingRequestValidator (FluentValidation).
public class CreateBookingRequest
{
    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }
}
