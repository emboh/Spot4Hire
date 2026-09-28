using Spot4Hire.Backend.Common;
using Spot4Hire.Backend.Dtos.Bookings;
using Spot4Hire.Backend.Dtos.Common;
using Gridify;

namespace Spot4Hire.Backend.Services.Abstractions;

public interface IBookingService
{
    // Returns null when the parent unit does not exist (404 vs an empty page).
    Task<PagedResponse<BookingResponse>?> GetBookingsAsync(Guid unitId, GridifyQuery query, CancellationToken ct);

    Task<BookingResponse?> GetBookingAsync(Guid unitId, Guid id, CancellationToken ct);

    // userId is the authenticated caller, set by the controller from the current
    // principal (never from the request body).
    Task<Result<BookingResponse>> CreateBookingAsync(Guid unitId, Guid userId, CreateBookingRequest request, CancellationToken ct);

    // isAdmin lets an administrator modify a booking that is not their own.
    Task<Result> UpdateBookingAsync(Guid unitId, Guid id, Guid userId, bool isAdmin, UpdateBookingRequest request, CancellationToken ct);

    Task<Result> DeleteBookingAsync(Guid unitId, Guid id, Guid userId, bool isAdmin, CancellationToken ct);

    // Read-only check: can this unit be booked for this range right now?
    // Uses the same rules as Create/Update. Not a reservation: a POST can
    // still get a Conflict if someone books the slot in between.
    Task<Result> CheckSlotAsync(Guid unitId, DateTime startAt, DateTime endAt, CancellationToken ct);
}
