namespace Spot4Hire.Backend.Data.Entities;

public class OpeningHour
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid VenueId { get; set; }

    public Venue Venue { get; set; } = null!;

    public DayOfWeek Day { get; set; }

    public TimeOnly OpenTime { get; set; }

    public TimeOnly CloseTime { get; set; }
}
