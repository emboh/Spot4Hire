using FluentValidation;
using Spot4Hire.Backend.Dtos.Venues;

namespace Spot4Hire.Backend.Validators.Venues;

public sealed class NearbyVenuesQueryValidator : AbstractValidator<NearbyVenuesQuery>
{
    public const double MaxRadiusKm = 50;
    public const int MaxTake = 50;

    public NearbyVenuesQueryValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);

        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);

        RuleFor(x => x.RadiusKm).GreaterThan(0).LessThanOrEqualTo(MaxRadiusKm);

        RuleFor(x => x.VenueType).IsInEnum().When(x => x.VenueType.HasValue);

        RuleFor(x => x.Take).InclusiveBetween(1, MaxTake);
    }
}
