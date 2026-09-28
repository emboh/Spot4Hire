using FluentValidation;
using Spot4Hire.Backend.Dtos.Venues;

namespace Spot4Hire.Backend.Validators.Venues;

// Shared rules for create and update. UpdateVenueRequest inherits
// CreateVenueRequest, so the same rules apply to both through this generic base.
public abstract class VenueRequestValidator<T> : AbstractValidator<T>
    where T : CreateVenueRequest
{
    protected VenueRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.VenueType)
            .IsInEnum();

        RuleFor(x => x.Address)
            .MaximumLength(250);

        RuleFor(x => x.Description)
            .MaximumLength(200);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .When(x => x.Longitude.HasValue);

        // Coordinates must be provided together, or both left empty.
        RuleFor(x => x)
            .Must(x => x.Latitude.HasValue == x.Longitude.HasValue)
            .WithName("Coordinates")
            .WithMessage("Latitude and Longitude must both be provided, or both left empty.");
    }
}

public sealed class CreateVenueRequestValidator : VenueRequestValidator<CreateVenueRequest> { }

public sealed class UpdateVenueRequestValidator : VenueRequestValidator<UpdateVenueRequest> { }
