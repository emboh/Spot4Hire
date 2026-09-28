using System.ComponentModel.DataAnnotations;

namespace Spot4Hire.Backend.Dtos.Auth;

// For updating the current user's profile fields that MapIdentityApi's
// /manage/info does not cover (email/password are handled there already).
public class UpdateProfileRequest
{
    [MaxLength(250)]
    public string? Address { get; set; }
}
