using Spot4Hire.Backend.Data.Entities;
using Spot4Hire.Backend.Dtos.OpeningHours;

namespace Spot4Hire.Backend.Mapping;

public static class OpeningHourMappings
{
    public static OpeningHourResponse ToResponse(this OpeningHour openingHour) => new()
    {
        Id = openingHour.Id,
        VenueId = openingHour.VenueId,
        Day = openingHour.Day,
        OpenTime = openingHour.OpenTime,
        CloseTime = openingHour.CloseTime,
    };

    // VenueId is set by the service from the route, not here.
    public static OpeningHour ToEntity(this CreateOpeningHourRequest request) => new()
    {
        Day = request.Day,
        OpenTime = request.OpenTime,
        CloseTime = request.CloseTime,
    };

    public static void ApplyTo(this UpdateOpeningHourRequest request, OpeningHour openingHour)
    {
        openingHour.Day = request.Day;
        openingHour.OpenTime = request.OpenTime;
        openingHour.CloseTime = request.CloseTime;
    }
}
