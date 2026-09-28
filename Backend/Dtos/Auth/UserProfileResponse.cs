namespace Spot4Hire.Backend.Dtos.Auth;

// The current user's profile, including custom fields and roles, which the
// built-in Identity endpoints do not return.
public class UserProfileResponse
{
    public Guid Id { get; set; }

    public string? Email { get; set; }

    public string? UserName { get; set; }

    public string? Address { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = [];
}
