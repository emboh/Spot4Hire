using Spot4Hire.Backend.Domain;

namespace Spot4Hire.Backend.Dtos.Venues;

public class NearbyVenueResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public VenueType VenueType { get; set; }

    public string? Address { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    // Straight-line distance from the search origin, rounded to 0.1 km.
    public double DistanceKm { get; set; }
}
