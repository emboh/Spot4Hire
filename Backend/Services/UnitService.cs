using Microsoft.EntityFrameworkCore;
using Spot4Hire.Backend.Data;
using Spot4Hire.Backend.Dtos.Common;
using Spot4Hire.Backend.Dtos.Units;
using Spot4Hire.Backend.Mapping;
using Spot4Hire.Backend.Services.Abstractions;
using Gridify;
using Spot4Hire.Backend.Common;
using Spot4Hire.Backend.Data.Entities;

namespace Spot4Hire.Backend.Services;

// Unit is a child of Venue, so the parent is checked and every lookup is
// scoped to its venue.
public class UnitService(AppDbContext db) : IUnitService
{
    public async Task<PagedResponse<UnitResponse>?> GetUnitsAsync(Guid venueId, GridifyQuery query, CancellationToken ct)
    {
        // A missing (or soft-deleted) parent is a 404, not an empty list.
        if (!await db.Venues.AnyAsync(v => v.Id == venueId, ct))
        {
            return null;
        }

        return await db.Units.AsNoTracking()
            .Where(u => u.VenueId == venueId)
            .ToPagedResponseAsync(query, GridifyMappers.Unit, u => u.ToResponse(), "name", ct);
    }

    public async Task<UnitResponse?> GetUnitAsync(Guid venueId, Guid id, CancellationToken ct)
    {
        var unit = await db.Units
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && u.VenueId == venueId, ct);

        return unit?.ToResponse();
    }

    public async Task<UnitResponse?> CreateUnitAsync(Guid venueId, CreateUnitRequest request, CancellationToken ct)
    {
        // The unit cannot be attached to a venue that does not exist.
        if (!await db.Venues.AnyAsync(v => v.Id == venueId, ct))
        {
            return null;
        }

        var unit = request.ToEntity();
        unit.VenueId = venueId;

        db.Units.Add(unit);
        await db.SaveChangesAsync(ct);

        return unit.ToResponse();
    }

    public async Task<bool> UpdateUnitAsync(Guid venueId, Guid id, UpdateUnitRequest request, CancellationToken ct)
    {
        var unit = await db.Units.FirstOrDefaultAsync(u => u.Id == id && u.VenueId == venueId, ct);

        if (unit is null)
        {
            return false;
        }

        request.ApplyTo(unit);
        await db.SaveChangesAsync(ct);

        return true;
    }

    public async Task<bool> DeleteUnitAsync(Guid venueId, Guid id, CancellationToken ct)
    {
        var unit = await db.Units.FirstOrDefaultAsync(u => u.Id == id && u.VenueId == venueId, ct);

        if (unit is null)
        {
            return false;
        }

        // Turned into a soft delete (DeletedAt set) by AppDbContext.SaveChanges.
        db.Units.Remove(unit);
        await db.SaveChangesAsync(ct);

        return true;
    }
}
