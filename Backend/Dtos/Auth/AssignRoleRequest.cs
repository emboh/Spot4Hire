using System.ComponentModel.DataAnnotations;
using Spot4Hire.Backend.Authorization;

namespace Spot4Hire.Backend.Dtos.Auth;

// For an admin endpoint that assigns a role to a user (user id from the route).
// AllowedValues (.NET 8+) validates against the defined role constants.
public class AssignRoleRequest
{
    [Required]
    [AllowedValues(Roles.Admin, Roles.Owner, Roles.Customer)]
    public string Role { get; set; } = string.Empty;
}
