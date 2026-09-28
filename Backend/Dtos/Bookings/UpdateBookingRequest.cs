namespace Spot4Hire.Backend.Dtos.Bookings;

// Identical to CreateBookingRequest for now (a reschedule changes start/end),
// so it inherits to avoid duplication. Split to a shared base if they diverge.
public class UpdateBookingRequest : CreateBookingRequest
{
}
