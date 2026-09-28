namespace Spot4Hire.Backend.Dtos.Venues;

// Currently identical to CreateVenueRequest, so it inherits to avoid duplication.
// If update ever needs to add/remove/relax a field, switch both to a shared
// abstract base (e.g. VenueRequestBase) instead of extending the create request.
public class UpdateVenueRequest : CreateVenueRequest
{
}
