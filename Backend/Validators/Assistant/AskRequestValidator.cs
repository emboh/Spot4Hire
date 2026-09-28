using FluentValidation;
using Spot4Hire.Backend.Dtos.Assistant;

namespace Spot4Hire.Backend.Validators.Assistant;

public sealed class AskRequestValidator : AbstractValidator<AskRequest>
{
    public AskRequestValidator()
    {
        // Keeps prompts (and token cost) small.
        RuleFor(x => x.Question)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .When(x => x.Longitude.HasValue);

        RuleFor(x => x)
            .Must(x => x.Latitude.HasValue == x.Longitude.HasValue)
            .WithName("Location")
            .WithMessage("Latitude and Longitude must both be provided, or both left empty.");
    }
}
