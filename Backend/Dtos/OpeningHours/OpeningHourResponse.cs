namespace Spot4Hire.Backend.Dtos.OpeningHours;

public class OpeningHourResponse
{
    public Guid Id { get; set; }

    public Guid VenueId { get; set; }

    public DayOfWeek Day { get; set; }

    public TimeOnly OpenTime { get; set; }

    public TimeOnly CloseTime { get; set; }
}
