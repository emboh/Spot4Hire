using Spot4Hire.Backend.Dtos.OpeningHours;
using Gridify;

namespace Spot4Hire.Backend.Services.Abstractions;

public interface IOpeningHourService
{
    // Returns null when the parent venue does not exist. The list itself is
    // small and bounded (by weekday), so it is not paginated.
    Task<IReadOnlyList<OpeningHourResponse>?> GetOpeningHoursAsync(Guid venueId, GridifyQuery query, CancellationToken ct);

    Task<OpeningHourResponse?> GetOpeningHourAsync(Guid venueId, Guid id, CancellationToken ct);

    // Returns null when the parent venue does not exist.
    Task<OpeningHourResponse?> CreateOpeningHourAsync(Guid venueId, CreateOpeningHourRequest request, CancellationToken ct);

    Task<bool> UpdateOpeningHourAsync(Guid venueId, Guid id, UpdateOpeningHourRequest request, CancellationToken ct);

    Task<bool> DeleteOpeningHourAsync(Guid venueId, Guid id, CancellationToken ct);
}
