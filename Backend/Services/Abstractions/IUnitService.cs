using Spot4Hire.Backend.Dtos.Common;
using Gridify;
using Spot4Hire.Backend.Dtos.Units;

namespace Spot4Hire.Backend.Services.Abstractions;

public interface IUnitService
{
    // Returns null when the parent venue does not exist (404 vs an empty page).
    Task<PagedResponse<UnitResponse>?> GetUnitsAsync(Guid venueId, GridifyQuery query, CancellationToken ct);

    Task<UnitResponse?> GetUnitAsync(Guid venueId, Guid id, CancellationToken ct);

    // Returns null when the parent venue does not exist.
    Task<UnitResponse?> CreateUnitAsync(Guid venueId, CreateUnitRequest request, CancellationToken ct);

    Task<bool> UpdateUnitAsync(Guid venueId, Guid id, UpdateUnitRequest request, CancellationToken ct);

    Task<bool> DeleteUnitAsync(Guid venueId, Guid id, CancellationToken ct);
}
