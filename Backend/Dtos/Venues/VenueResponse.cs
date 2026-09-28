using System.Text.Json.Serialization;
using Spot4Hire.Backend.Domain;
using Spot4Hire.Backend.Dtos.OpeningHours;

namespace Spot4Hire.Backend.Dtos.Venues;

public class VenueResponse
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public VenueType VenueType { get; set; }

    public string? Address { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Populated only when the caller asks (includeOpeningHours=true); otherwise
    // left null and dropped from the JSON.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<OpeningHourResponse>? OpeningHours { get; set; }
}
