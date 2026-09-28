namespace Spot4Hire.Backend.Dtos.Units;

// Identical to CreateUnitRequest for now, so it inherits to avoid duplication.
// VenueId is not in the body (it comes from the route), so a unit cannot be
// moved to another venue via update. If the requests diverge, switch both to a
// shared abstract base instead of extending the create request.
public class UpdateUnitRequest : CreateUnitRequest
{
}
