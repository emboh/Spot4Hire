using FluentValidation;
using Spot4Hire.Backend.Dtos.Units;

namespace Spot4Hire.Backend.Validators.Units;

// Shared rules for create and update. UpdateUnitRequest inherits CreateUnitRequest,
// so the same rules apply to both through this generic base.
public abstract class UnitRequestValidator<T> : AbstractValidator<T>
    where T : CreateUnitRequest
{
    protected UnitRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.Price.HasValue);

        RuleFor(x => x.Currency)
            .MaximumLength(3);

        RuleFor(x => x.Description)
            .MaximumLength(200);

        RuleFor(x => x.MinBookingDurationMinutes)
            .InclusiveBetween(1, 1440);
    }
}

public sealed class CreateUnitRequestValidator : UnitRequestValidator<CreateUnitRequest> { }

public sealed class UpdateUnitRequestValidator : UnitRequestValidator<UpdateUnitRequest> { }
