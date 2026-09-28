using Spot4Hire.Backend.Domain;

namespace Spot4Hire.Backend.Dtos.Venues;

// Query string for GET api/venues/nearby.
public class NearbyVenuesQuery
{
    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double RadiusKm { get; set; } = 5;

    public VenueType? VenueType { get; set; }

    public int Take { get; set; } = 20;
}
