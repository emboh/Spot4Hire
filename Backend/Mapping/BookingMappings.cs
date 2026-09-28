using Spot4Hire.Backend.Data.Entities;
using Spot4Hire.Backend.Dtos.Bookings;

namespace Spot4Hire.Backend.Mapping;

public static class BookingMappings
{
    public static BookingResponse ToResponse(this Booking booking) => new()
    {
        Id = booking.Id,
        UnitId = booking.UnitId,
        UserId = booking.UserId,
        StartAt = booking.StartAt,
        EndAt = booking.EndAt,
        DurationMinutes = (int)booking.Duration.TotalMinutes,
    };

    // UnitId is set by the service from the route, not here.
    public static Booking ToEntity(this CreateBookingRequest request) => new()
    {
        StartAt = request.StartAt,
        EndAt = request.EndAt,
    };

    public static void ApplyTo(this UpdateBookingRequest request, Booking booking)
    {
        booking.StartAt = request.StartAt;
        booking.EndAt = request.EndAt;
    }
}
