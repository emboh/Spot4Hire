using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gridify;
using Spot4Hire.Backend.Authorization;
using Spot4Hire.Backend.Dtos.Common;
using Spot4Hire.Backend.Dtos.Units;
using Spot4Hire.Backend.Services.Abstractions;

namespace Spot4Hire.Backend.Controllers;

// Units are a child resource of a Venue, so the route is nested.
[ApiController]
[Route("api/venues/{venueId:guid}/units")]
public class UnitsController(IUnitService units) : ControllerBase
{
    // GET api/venues/{venueId}/units?page=1&pageSize=20
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponse<UnitResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<UnitResponse>>> GetUnits(
        Guid venueId,
        [FromQuery] GridifyQuery query,
        CancellationToken ct)
    {
        var result = await units.GetUnitsAsync(venueId, query, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // GET api/venues/{venueId}/units/{id}
    [HttpGet("{id:guid}", Name = nameof(GetUnit))]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UnitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UnitResponse>> GetUnit(Guid venueId, Guid id, CancellationToken ct)
    {
        var unit = await units.GetUnitAsync(venueId, id, ct);
        return unit is null ? NotFound() : Ok(unit);
    }

    // POST api/venues/{venueId}/units
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Owner}")]
    [ProducesResponseType(typeof(UnitResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UnitResponse>> CreateUnit(Guid venueId, CreateUnitRequest request, CancellationToken ct)
    {
        var unit = await units.CreateUnitAsync(venueId, request, ct);
        return unit is null
            ? NotFound()
            : CreatedAtAction(nameof(GetUnit), new { venueId, id = unit.Id }, unit);
    }

    // PUT api/venues/{venueId}/units/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Owner}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUnit(Guid venueId, Guid id, UpdateUnitRequest request, CancellationToken ct)
        => await units.UpdateUnitAsync(venueId, id, request, ct) ? NoContent() : NotFound();

    // DELETE api/venues/{venueId}/units/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Owner}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUnit(Guid venueId, Guid id, CancellationToken ct)
        => await units.DeleteUnitAsync(venueId, id, ct) ? NoContent() : NotFound();
}
