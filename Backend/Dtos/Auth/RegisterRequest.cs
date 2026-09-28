using System.ComponentModel.DataAnnotations;

namespace Spot4Hire.Backend.Dtos.Auth;

// For a custom registration endpoint that captures the profile Address and
// assigns a default role server-side. The built-in MapIdentityApi /register
// only accepts email + password and cannot set custom ApplicationUser fields.
// No role field on purpose: users must not be able to self-assign a role.
public class RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    // Password strength is enforced by Identity's options in UserManager,
    // so it is not duplicated here.

    [MaxLength(250)]
    public string? Address { get; set; }
}
