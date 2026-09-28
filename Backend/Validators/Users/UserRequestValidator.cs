using FluentValidation;
using Spot4Hire.Backend.Authorization;
using Spot4Hire.Backend.Dtos.Users;

namespace Spot4Hire.Backend.Validators.Users;

// Shared rules for create and update. Both requests derive from UserRequestBase.
public abstract class UserRequestValidator<T> : AbstractValidator<T>
    where T : UserRequestBase
{
    protected UserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Address)
            .MaximumLength(250);

        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => Roles.All.Contains(role))
            .WithMessage($"Role must be one of: {string.Join(", ", Roles.All)}.");
    }
}

public sealed class UpdateUserRequestValidator : UserRequestValidator<UpdateUserRequest> { }

public sealed class CreateUserRequestValidator : UserRequestValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
