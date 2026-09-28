namespace Spot4Hire.Backend.Dtos.Users;

// Admin-only. Validation lives in CreateUserRequestValidator.
public class CreateUserRequest : UserRequestBase
{
    public string Password { get; set; } = string.Empty;
}
