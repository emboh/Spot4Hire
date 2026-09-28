using Spot4Hire.Backend.Data.Entities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Spot4Hire.Backend.Data.Entities;

public class Unit : IAuditable, ISoftDeletable
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid VenueId { get; set; }

    public Venue Venue { get; set; } = null!;

    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Price { get; set; }

    [MaxLength(3)]
    public string? Currency { get; set; } = "IDR";

    [MaxLength(200)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public TimeSpan MinBookingDuration { get; set; } = TimeSpan.FromHours(1);

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
