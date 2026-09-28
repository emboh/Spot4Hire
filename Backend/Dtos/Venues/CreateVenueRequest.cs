using Spot4Hire.Backend.Domain;

namespace Spot4Hire.Backend.Dtos.Venues;

// Validation lives in CreateVenueRequestValidator (FluentValidation), which is
// the single source of truth for this request.
public class CreateVenueRequest
{
    public string Name { get; set; } = string.Empty;

    public VenueType VenueType { get; set; }

    public string? Address { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? Description { get; set; }

    // Owner to assign. Only honored when an admin creates the venue; when an
    // owner creates their own venue this is ignored and the owner is the caller.
    public Guid? UserId { get; set; }
}
