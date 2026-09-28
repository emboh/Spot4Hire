using Gridify;
using Spot4Hire.Backend.Data.Entities;

namespace Spot4Hire.Backend.Common;

// One place that defines which fields are filterable/sortable per entity.
// Used by the services (for querying) and by the OpenAPI transformer (for docs).
public static class GridifyMappers
{
    public static readonly IGridifyMapper<Venue> Venue = new GridifyMapper<Venue>()
        .AddMap("name", v => v.Name)
        .AddMap("venueType", v => v.VenueType)
        .AddMap("address", v => v.Address)
        .AddMap("description", v => v.Description)
        .AddMap("createdAt", v => v.CreatedAt);

    public static readonly IGridifyMapper<Unit> Unit = new GridifyMapper<Unit>()
        .AddMap("name", u => u.Name)
        .AddMap("price", u => u.Price)
        .AddMap("currency", u => u.Currency)
        .AddMap("createdAt", u => u.CreatedAt);

    public static readonly IGridifyMapper<Booking> Booking = new GridifyMapper<Booking>()
        .AddMap("startAt", b => b.StartAt)
        .AddMap("endAt", b => b.EndAt)
        .AddMap("userId", b => b.UserId);

    public static readonly IGridifyMapper<ApplicationUser> User = new GridifyMapper<ApplicationUser>()
        .AddMap("userName", u => u.UserName)
        .AddMap("email", u => u.Email)
        .AddMap("address", u => u.Address);

    public static readonly IGridifyMapper<OpeningHour> OpeningHour = new GridifyMapper<OpeningHour>()
        .AddMap("day", o => o.Day)
        .AddMap("openTime", o => o.OpenTime)
        .AddMap("closeTime", o => o.CloseTime);
}
