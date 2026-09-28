using FluentValidation;
using Spot4Hire.Backend.Dtos.Users;

namespace Spot4Hire.Backend.Validators.Users;

public sealed class SetPasswordRequestValidator : AbstractValidator<SetPasswordRequest>
{
    public SetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty();
    }
}
