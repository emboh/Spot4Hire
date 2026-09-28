using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gridify;
using Spot4Hire.Backend.Authorization;
using Spot4Hire.Backend.Common;
using Spot4Hire.Backend.Dtos.Common;
using Spot4Hire.Backend.Dtos.Venues;
using Spot4Hire.Backend.Services.Abstractions;

namespace Spot4Hire.Backend.Controllers;

[ApiController]
[Route("api/venues")]
public class VenuesController(IVenueService venues) : ControllerBase
{
    // GET api/venues?page=1&pageSize=20
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponse<VenueResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<VenueResponse>>> GetVenues(
        [FromQuery] GridifyQuery query,
        [FromQuery] bool includeOpeningHours,
        CancellationToken ct)
        => Ok(await venues.GetVenuesAsync(query, includeOpeningHours, ct));

    // GET api/venues/nearby?latitude=-7.26&longitude=112.75&radiusKm=5&venueType=Cafe&take=20
    [HttpGet("nearby")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<NearbyVenueResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<NearbyVenueResponse>>> GetNearbyVenues(
        [FromQuery] NearbyVenuesQuery query,
        CancellationToken ct)
        => Ok(await venues.GetNearbyVenuesAsync(
            query.Latitude, query.Longitude, query.RadiusKm * 1000, query.VenueType, query.Take, ct));

    // GET api/venues/{id}
    [HttpGet("{id:guid}", Name = nameof(GetVenue))]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VenueResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VenueResponse>> GetVenue(Guid id, [FromQuery] bool includeOpeningHours, CancellationToken ct)
    {
        var venue = await venues.GetVenueAsync(id, includeOpeningHours, ct);

        return venue is null ? NotFound() : Ok(venue);
    }

    // POST api/venues
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Owner}")]
    [ProducesResponseType(typeof(VenueResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<VenueResponse>> CreateVenue(CreateVenueRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await venues.CreateVenueAsync(request, userId, User.IsInRole(Roles.Admin), ct);
        if (!result.IsSuccess)
        {
            return Failure(result);
        }

        var venue = result.Value!;
        return CreatedAtAction(nameof(GetVenue), new { id = venue.Id }, venue);
    }

    // PUT api/venues/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Owner}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateVenue(Guid id, UpdateVenueRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await venues.UpdateVenueAsync(id, request, userId, User.IsInRole(Roles.Admin), ct);
        return result.IsSuccess ? NoContent() : Failure(result);
    }

    // DELETE api/venues/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Owner}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVenue(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await venues.DeleteVenueAsync(id, userId, User.IsInRole(Roles.Admin), ct);
        return result.IsSuccess ? NoContent() : Failure(result);
    }

    private bool TryGetUserId(out Guid userId)
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private ActionResult Failure(Result result) => result.Status switch
    {
        ResultStatus.NotFound => NotFound(),
        ResultStatus.Forbidden => Forbid(),
        ResultStatus.Conflict => Conflict(new ProblemDetails { Detail = result.Error }),
        ResultStatus.Invalid => UnprocessableEntity(new ProblemDetails { Detail = result.Error }),
        _ => Problem(),
    };
}
