using Spot4Hire.Backend.Data.Entities;
using Spot4Hire.Backend.Dtos.Users;

namespace Spot4Hire.Backend.Mapping;

public static class UserMappings
{
    // Roles are fetched separately (via UserManager) and passed in.
    public static UserResponse ToResponse(this ApplicationUser user, IEnumerable<string> roles) => new()
    {
        Id = user.Id,
        Email = user.Email,
        UserName = user.UserName,
        Address = user.Address,
        Roles = roles.ToList(),
    };
}
