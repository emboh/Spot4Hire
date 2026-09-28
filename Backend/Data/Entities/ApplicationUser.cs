using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Spot4Hire.Backend.Data.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    [MaxLength(250)]
    public string? Address { get; set; }
}
