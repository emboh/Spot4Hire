using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;
using Spot4Hire.Backend.Data.Entities.Common;
using Spot4Hire.Backend.Domain;

namespace Spot4Hire.Backend.Data.Entities;

public class Venue : IAuditable, ISoftDeletable
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public VenueType VenueType { get; set; }

    [MaxLength(250)]
    public string? Address { get; set; }

    public Point? Coordinates { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<OpeningHour> OpeningHours { get; set; } = new List<OpeningHour>();

    public ICollection<Unit> Units { get; set; } = new List<Unit>();
}