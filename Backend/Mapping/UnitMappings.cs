using Spot4Hire.Backend.Data.Entities;
using Spot4Hire.Backend.Dtos.Units;

namespace Spot4Hire.Backend.Mapping;

public static class UnitMappings
{
    public static UnitResponse ToResponse(this Unit unit) => new()
    {
        Id = unit.Id,
        VenueId = unit.VenueId,
        Name = unit.Name,
        Price = unit.Price,
        Currency = unit.Currency,
        Description = unit.Description,
        MinBookingDurationMinutes = (int)unit.MinBookingDuration.TotalMinutes,
        CreatedAt = unit.CreatedAt,
        UpdatedAt = unit.UpdatedAt,
    };

    // VenueId is set by the controller from the route, not here.
    public static Unit ToEntity(this CreateUnitRequest request) => new()
    {
        Name = request.Name,
        Price = request.Price,
        Currency = request.Currency,
        Description = request.Description,
        MinBookingDuration = TimeSpan.FromMinutes(request.MinBookingDurationMinutes),
    };

    public static void ApplyTo(this UpdateUnitRequest request, Unit unit)
    {
        unit.Name = request.Name;
        unit.Price = request.Price;
        unit.Currency = request.Currency;
        unit.Description = request.Description;
        unit.MinBookingDuration = TimeSpan.FromMinutes(request.MinBookingDurationMinutes);
    }
}
