using Spot4Hire.Backend.Common;
using Spot4Hire.Backend.Dtos.Common;
using Gridify;
using Spot4Hire.Backend.Domain;
using Spot4Hire.Backend.Dtos.Venues;

namespace Spot4Hire.Backend.Services.Abstractions;

public interface IVenueService
{
    Task<PagedResponse<VenueResponse>> GetVenuesAsync(GridifyQuery query, bool includeOpeningHours, CancellationToken ct);

    Task<VenueResponse?> GetVenueAsync(Guid id, bool includeOpeningHours, CancellationToken ct);

    // Venues within radiusMeters of the given point, nearest first. Venues
    // without coordinates are skipped.
    Task<IReadOnlyList<NearbyVenueResponse>> GetNearbyVenuesAsync(
        double latitude, double longitude, double radiusMeters, VenueType? venueType, int take, CancellationToken ct);

    // currentUserId and isAdmin come from the controller. An admin must supply
    // request.UserId (the owner); an owner becomes the owner of their own venue.
    Task<Result<VenueResponse>> CreateVenueAsync(CreateVenueRequest request, Guid currentUserId, bool isAdmin, CancellationToken ct);

    // A non-admin may only manage venues they own; reassigning the owner is admin-only.
    Task<Result> UpdateVenueAsync(Guid id, UpdateVenueRequest request, Guid currentUserId, bool isAdmin, CancellationToken ct);

    Task<Result> DeleteVenueAsync(Guid id, Guid currentUserId, bool isAdmin, CancellationToken ct);
}
