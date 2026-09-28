using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Spot4Hire.Backend.Authorization;
using Spot4Hire.Backend.Common;
using Spot4Hire.Backend.Data;
using Spot4Hire.Backend.Data.Entities;
using Spot4Hire.Backend.Domain;
using Spot4Hire.Backend.Dtos.Common;
using Spot4Hire.Backend.Dtos.Venues;
using Spot4Hire.Backend.Mapping;
using Spot4Hire.Backend.Services.Abstractions;
using Gridify;

namespace Spot4Hire.Backend.Services;

public class VenueService(AppDbContext db, UserManager<ApplicationUser> userManager) : IVenueService
{
    public async Task<PagedResponse<VenueResponse>> GetVenuesAsync(GridifyQuery query, bool includeOpeningHours, CancellationToken ct)
    {
        IQueryable<Venue> baseQuery = db.Venues.AsNoTracking();
        if (includeOpeningHours)
        {
            baseQuery = baseQuery.Include(v => v.OpeningHours);
        }

        return await baseQuery.ToPagedResponseAsync(query, GridifyMappers.Venue, v => v.ToResponse(includeOpeningHours), "name", ct);
    }

    public async Task<VenueResponse?> GetVenueAsync(Guid id, bool includeOpeningHours, CancellationToken ct)
    {
        IQueryable<Venue> baseQuery = db.Venues.AsNoTracking();
        if (includeOpeningHours)
        {
            baseQuery = baseQuery.Include(v => v.OpeningHours);
        }

        var venue = await baseQuery.FirstOrDefaultAsync(v => v.Id == id, ct);

        return venue?.ToResponse(includeOpeningHours);
    }

    public async Task<IReadOnlyList<NearbyVenueResponse>> GetNearbyVenuesAsync(
        double latitude, double longitude, double radiusMeters, VenueType? venueType, int take, CancellationToken ct)
    {
        var origin = VenueMappings.ToPoint(latitude, longitude)!;

        // The column is SQL Server geography (SRID 4326), so distances are in meters.
        // IsWithinDistance and Distance translate to STDistance.
        var query = db.Venues
            .AsNoTracking()
            .Where(v => v.Coordinates != null && v.Coordinates.IsWithinDistance(origin, radiusMeters));

        if (venueType is not null)
        {
            query = query.Where(v => v.VenueType == venueType);
        }

        var rows = await query
            .OrderBy(v => v.Coordinates!.Distance(origin))
            .Take(take)
            .Select(v => new { Venue = v, DistanceMeters = v.Coordinates!.Distance(origin) })
            .ToListAsync(ct);

        return rows
            .Select(r => r.Venue.ToNearbyResponse(r.DistanceMeters))
            .ToList();
    }

    public async Task<Result<VenueResponse>> CreateVenueAsync(CreateVenueRequest request, Guid currentUserId, bool isAdmin, CancellationToken ct)
    {
        Guid ownerId;
        if (isAdmin)
        {
            if (request.UserId is null)
            {
                return Result<VenueResponse>.Invalid("An owner (UserId) is required when an admin creates a venue.");
            }

            ownerId = request.UserId.Value;
            if (!await IsOwnerAsync(ownerId))
            {
                return Result<VenueResponse>.Invalid("The assigned user must be an existing user with the Owner role.");
            }
        }
        else
        {
            // An owner owns the venue they create; any UserId in the body is ignored.
            ownerId = currentUserId;
        }

        var venue = request.ToEntity();
        venue.UserId = ownerId;

        db.Venues.Add(venue);
        await db.SaveChangesAsync(ct);

        return Result<VenueResponse>.Success(venue.ToResponse());
    }

    public async Task<Result> UpdateVenueAsync(Guid id, UpdateVenueRequest request, Guid currentUserId, bool isAdmin, CancellationToken ct)
    {
        var venue = await db.Venues.FirstOrDefaultAsync(v => v.Id == id, ct);

        if (venue is null)
        {
            return Result.NotFound();
        }

        if (!isAdmin && venue.UserId != currentUserId)
        {
            return Result.Forbidden("You can only manage your own venues.");
        }

        // Reassigning the owner is admin-only, and only to a user with the Owner role.
        if (request.UserId is { } newOwnerId && newOwnerId != venue.UserId)
        {
            if (!isAdmin)
            {
                return Result.Forbidden("Only an admin can change the venue owner.");
            }

            if (!await IsOwnerAsync(newOwnerId))
            {
                return Result.Invalid("The assigned user must be an existing user with the Owner role.");
            }

            venue.UserId = newOwnerId;
        }

        request.ApplyTo(venue);
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> DeleteVenueAsync(Guid id, Guid currentUserId, bool isAdmin, CancellationToken ct)
    {
        var venue = await db.Venues.FirstOrDefaultAsync(v => v.Id == id, ct);

        if (venue is null)
        {
            return Result.NotFound();
        }

        if (!isAdmin && venue.UserId != currentUserId)
        {
            return Result.Forbidden("You can only manage your own venues.");
        }

        // Cascade the soft delete to this venue's units, in the same SaveChanges
        // so it commits as one transaction. Bookings are not soft-deletable
        // (they expire by their date), so there is nothing to cascade below units.
        var units = await db.Units.Where(u => u.VenueId == id).ToListAsync(ct);
        db.Units.RemoveRange(units);
        db.Venues.Remove(venue);

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // True only when the user exists and holds the Owner role.
    private async Task<bool> IsOwnerAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is not null && await userManager.IsInRoleAsync(user, Roles.Owner);
    }
}
