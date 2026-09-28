using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gridify;
using Spot4Hire.Backend.Authorization;
using Spot4Hire.Backend.Dtos.OpeningHours;
using Spot4Hire.Backend.Services.Abstractions;

namespace Spot4Hire.Backend.Controllers;

// Opening hours are a child resource of a Venue, so the route is nested.
[ApiController]
[Route("api/venues/{venueId:guid}/opening-hours")]
public class OpeningHoursController(IOpeningHourService openingHours) : ControllerBase
{
    // GET api/venues/{venueId}/opening-hours
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<OpeningHourResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OpeningHourResponse>>> GetOpeningHours(Guid venueId, [FromQuery] GridifyQuery query, CancellationToken ct)
    {
        var result = await openingHours.GetOpeningHoursAsync(venueId, query, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // GET api/venues/{venueId}/opening-hours/{id}
    [HttpGet("{id:guid}", Name = nameof(GetOpeningHour))]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OpeningHourResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OpeningHourResponse>> GetOpeningHour(Guid venueId, Guid id, CancellationToken ct)
    {
        var openingHour = await openingHours.GetOpeningHourAsync(venueId, id, ct);
        return openingHour is null ? NotFound() : Ok(openingHour);
    }

    // POST api/venues/{venueId}/opening-hours
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Owner}")]
    [ProducesResponseType(typeof(OpeningHourResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OpeningHourResponse>> CreateOpeningHour(Guid venueId, CreateOpeningHourRequest request, CancellationToken ct)
    {
        var openingHour = await openingHours.CreateOpeningHourAsync(venueId, request, ct);
        return openingHour is null
            ? NotFound()
            : CreatedAtAction(nameof(GetOpeningHour), new { venueId, id = openingHour.Id }, openingHour);
    }

    // PUT api/venues/{venueId}/opening-hours/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Owner}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOpeningHour(Guid venueId, Guid id, UpdateOpeningHourRequest request, CancellationToken ct)
        => await openingHours.UpdateOpeningHourAsync(venueId, id, request, ct) ? NoContent() : NotFound();

    // DELETE api/venues/{venueId}/opening-hours/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Owner}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOpeningHour(Guid venueId, Guid id, CancellationToken ct)
        => await openingHours.DeleteOpeningHourAsync(venueId, id, ct) ? NoContent() : NotFound();
}
