namespace Spot4Hire.Backend.Dtos.Users;

public class UserResponse
{
    public Guid Id { get; set; }

    public string? Email { get; set; }

    public string? UserName { get; set; }

    public string? Address { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = [];
}
