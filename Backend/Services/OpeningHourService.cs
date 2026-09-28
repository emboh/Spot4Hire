using Microsoft.EntityFrameworkCore;
using Spot4Hire.Backend.Data;
using Spot4Hire.Backend.Dtos.OpeningHours;
using Spot4Hire.Backend.Mapping;
using Spot4Hire.Backend.Common;
using Spot4Hire.Backend.Services.Abstractions;
using Gridify;
using Spot4Hire.Backend.Data.Entities;

namespace Spot4Hire.Backend.Services;

// OpeningHour is a child of Venue, so the parent is checked and every lookup is
// scoped to its venue.
public class OpeningHourService(AppDbContext db) : IOpeningHourService
{
    public async Task<IReadOnlyList<OpeningHourResponse>?> GetOpeningHoursAsync(Guid venueId, GridifyQuery query, CancellationToken ct)
    {
        // A missing (or soft-deleted) parent is a 404, not an empty list.
        if (!await db.Venues.AnyAsync(v => v.Id == venueId, ct))
        {
            return null;
        }

        var hoursQuery = db.OpeningHours.AsNoTracking()
            .Where(o => o.VenueId == venueId)
            .ApplyFiltering(query, GridifyMappers.OpeningHour);

        // Small, bounded list: filter and sort but do not paginate.
        hoursQuery = string.IsNullOrWhiteSpace(query.OrderBy)
            ? hoursQuery.OrderBy(o => o.Day).ThenBy(o => o.OpenTime)
            : hoursQuery.ApplyOrdering(query, GridifyMappers.OpeningHour);

        var hours = await hoursQuery.ToListAsync(ct);
        return hours.Select(o => o.ToResponse()).ToList();
    }

    public async Task<OpeningHourResponse?> GetOpeningHourAsync(Guid venueId, Guid id, CancellationToken ct)
    {
        var openingHour = await db.OpeningHours
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id && o.VenueId == venueId, ct);

        return openingHour?.ToResponse();
    }

    public async Task<OpeningHourResponse?> CreateOpeningHourAsync(Guid venueId, CreateOpeningHourRequest request, CancellationToken ct)
    {
        if (!await db.Venues.AnyAsync(v => v.Id == venueId, ct))
        {
            return null;
        }

        var openingHour = request.ToEntity();
        openingHour.VenueId = venueId;

        db.OpeningHours.Add(openingHour);
        await db.SaveChangesAsync(ct);

        return openingHour.ToResponse();
    }

    public async Task<bool> UpdateOpeningHourAsync(Guid venueId, Guid id, UpdateOpeningHourRequest request, CancellationToken ct)
    {
        var openingHour = await db.OpeningHours.FirstOrDefaultAsync(o => o.Id == id && o.VenueId == venueId, ct);

        if (openingHour is null)
        {
            return false;
        }

        request.ApplyTo(openingHour);
        await db.SaveChangesAsync(ct);

        return true;
    }

    public async Task<bool> DeleteOpeningHourAsync(Guid venueId, Guid id, CancellationToken ct)
    {
        var openingHour = await db.OpeningHours.FirstOrDefaultAsync(o => o.Id == id && o.VenueId == venueId, ct);

        if (openingHour is null)
        {
            return false;
        }

        // Hard delete: OpeningHour is not soft-deletable (it is plain config data).
        db.OpeningHours.Remove(openingHour);
        await db.SaveChangesAsync(ct);

        return true;
    }
}
