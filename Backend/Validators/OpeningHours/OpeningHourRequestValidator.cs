using FluentValidation;
using Spot4Hire.Backend.Dtos.OpeningHours;

namespace Spot4Hire.Backend.Validators.OpeningHours;

// Shared rules for create and update. UpdateOpeningHourRequest inherits
// CreateOpeningHourRequest, so both use this generic base.
public abstract class OpeningHourRequestValidator<T> : AbstractValidator<T>
    where T : CreateOpeningHourRequest
{
    protected OpeningHourRequestValidator()
    {
        RuleFor(x => x.Day)
            .IsInEnum();

        // Same-day hours only. Overnight ranges (e.g. 22:00-02:00) are not
        // supported by this rule and would need a different model.
        RuleFor(x => x.CloseTime)
            .GreaterThan(x => x.OpenTime)
            .WithMessage("Close time must be after open time.");
    }
}

public sealed class CreateOpeningHourRequestValidator : OpeningHourRequestValidator<CreateOpeningHourRequest> { }

public sealed class UpdateOpeningHourRequestValidator : OpeningHourRequestValidator<UpdateOpeningHourRequest> { }
