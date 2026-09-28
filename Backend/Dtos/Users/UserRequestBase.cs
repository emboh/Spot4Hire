namespace Spot4Hire.Backend.Dtos.Users;

// Shared fields for admin create/update of a user.
public abstract class UserRequestBase
{
    public string Email { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string Role { get; set; } = string.Empty;
}
