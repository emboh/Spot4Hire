using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gridify;
using Spot4Hire.Backend.Authorization;
using Spot4Hire.Backend.Common;
using Spot4Hire.Backend.Dtos.Bookings;
using Spot4Hire.Backend.Dtos.Common;
using Spot4Hire.Backend.Services.Abstractions;

namespace Spot4Hire.Backend.Controllers;

// Booking is a child of Unit. A Unit id is globally unique, so bookings nest
// directly under the unit rather than repeating the venue in the path.
[ApiController]
[Route("api/units/{unitId:guid}/bookings")]
public class BookingsController(IBookingService bookings) : ControllerBase
{
    // GET api/units/{unitId}/bookings?page=1&pageSize=20
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponse<BookingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<BookingResponse>>> GetBookings(
        Guid unitId,
        [FromQuery] GridifyQuery query,
        CancellationToken ct)
    {
        var result = await bookings.GetBookingsAsync(unitId, query, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // GET api/units/{unitId}/bookings/{id}
    [HttpGet("{id:guid}", Name = nameof(GetBooking))]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingResponse>> GetBooking(Guid unitId, Guid id, CancellationToken ct)
    {
        var booking = await bookings.GetBookingAsync(unitId, id, ct);
        return booking is null ? NotFound() : Ok(booking);
    }

    // POST api/units/{unitId}/bookings
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BookingResponse>> CreateBooking(Guid unitId, CreateBookingRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await bookings.CreateBookingAsync(unitId, userId, request, ct);
        if (!result.IsSuccess)
        {
            return Failure(result);
        }

        var booking = result.Value!;
        return CreatedAtAction(nameof(GetBooking), new { unitId, id = booking.Id }, booking);
    }

    // PUT api/units/{unitId}/bookings/{id}
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateBooking(Guid unitId, Guid id, UpdateBookingRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await bookings.UpdateBookingAsync(unitId, id, userId, User.IsInRole(Roles.Admin), request, ct);
        return result.IsSuccess ? NoContent() : Failure(result);
    }

    // DELETE api/units/{unitId}/bookings/{id}
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBooking(Guid unitId, Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await bookings.DeleteBookingAsync(unitId, id, userId, User.IsInRole(Roles.Admin), ct);
        return result.IsSuccess ? NoContent() : Failure(result);
    }

    private bool TryGetUserId(out Guid userId)
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    // Maps a failed Result to the matching HTTP status.
    private ActionResult Failure(Result result) => result.Status switch
    {
        ResultStatus.NotFound => NotFound(),
        ResultStatus.Forbidden => Forbid(),
        ResultStatus.Conflict => Conflict(new ProblemDetails { Detail = result.Error }),
        ResultStatus.Invalid => UnprocessableEntity(new ProblemDetails { Detail = result.Error }),
        _ => Problem(),
    };
}
