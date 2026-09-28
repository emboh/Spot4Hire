using NetTopologySuite.Geometries;
using Spot4Hire.Backend.Data.Entities;
using Spot4Hire.Backend.Dtos.OpeningHours;
using Spot4Hire.Backend.Dtos.Venues;

namespace Spot4Hire.Backend.Mapping;

public static class VenueMappings
{
    // Coordinate reference system for latitude/longitude (WGS 84).
    private const int WGS84 = 4326;

    // Set includeOpeningHours only when venue.OpeningHours was loaded (Included).
    public static VenueResponse ToResponse(this Venue venue, bool includeOpeningHours = false) => new()
    {
        Id = venue.Id,
        UserId = venue.UserId,
        Name = venue.Name,
        VenueType = venue.VenueType,
        Address = venue.Address,
        Latitude = venue.Coordinates?.Y,
        Longitude = venue.Coordinates?.X,
        Description = venue.Description,
        CreatedAt = venue.CreatedAt,
        UpdatedAt = venue.UpdatedAt,
        OpeningHours = includeOpeningHours
            ? venue.OpeningHours
                .OrderBy(o => o.Day)
                .ThenBy(o => o.OpenTime)
                .Select(o => o.ToResponse())
                .ToList()
            : null,
    };

    // Distance comes from the query (STDistance, in meters); coordinates are
    // guaranteed present because the nearby query filters them out otherwise.
    public static NearbyVenueResponse ToNearbyResponse(this Venue venue, double distanceMeters) => new()
    {
        Id = venue.Id,
        Name = venue.Name,
        VenueType = venue.VenueType,
        Address = venue.Address,
        Latitude = venue.Coordinates!.Y,
        Longitude = venue.Coordinates.X,
        DistanceKm = Math.Round(distanceMeters / 1000, 1),
    };

    public static Venue ToEntity(this CreateVenueRequest request) => new()
    {
        Name = request.Name,
        VenueType = request.VenueType,
        Address = request.Address,
        Description = request.Description,
        Coordinates = ToPoint(request.Latitude, request.Longitude),
    };

    public static void ApplyTo(this UpdateVenueRequest request, Venue venue)
    {
        venue.Name = request.Name;
        venue.VenueType = request.VenueType;
        venue.Address = request.Address;
        venue.Description = request.Description;
        venue.Coordinates = ToPoint(request.Latitude, request.Longitude);
    }

    // NetTopologySuite takes X (longitude) first, then Y (latitude).
    public static Point? ToPoint(double? latitude, double? longitude)
        => latitude.HasValue && longitude.HasValue
            ? new Point(longitude.Value, latitude.Value) { SRID = WGS84 }
            : null;
}
