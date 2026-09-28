using System.ComponentModel;
using System.Globalization;
using Gridify;
using Spot4Hire.Backend.Common;
using Spot4Hire.Backend.Domain;
using Spot4Hire.Backend.Services.Abstractions;

namespace Spot4Hire.Backend.Services.Assistant;

// The functions the model can call. Each one is a thin, read-only wrapper
// around an existing service, so the assistant follows the same rules as the
// REST API. Results are small records to keep the prompt (and tokens) small.
//
// Errors are returned as data, not thrown: the model reads the message and
// explains it to the user.
//
// Tool parameters are never nullable (use "" as the default instead). MEAI
// writes a nullable parameter as "type": ["string", "null"] in the JSON schema,
// and OllamaSharp cannot read that array (JsonException when sending tools).
public sealed class AssistantTools(
    IVenueService venues,
    IUnitService units,
    IBookingService bookings,
    AssistantContext context)
{
    private const int MaxResults = 10;
    private const double DefaultRadiusKm = 5;
    private const double MaxRadiusKm = 50;

    private static readonly string VenueTypeNames = string.Join(", ", Enum.GetNames<VenueType>());

    [Description("Search venues by part of the name and/or by venue type. Returns up to 10 venues with their ids.")]
    public async Task<object> SearchVenues(
        [Description("Part of the venue name. Leave empty to list all.")] string name = "",
        [Description("Venue type: Rental, Cafe or Other. Leave empty for any type.")] string venueType = "",
        CancellationToken ct = default)
    {
        if (!TryParseVenueType(venueType, out var type))
        {
            return Error($"Unknown venue type '{venueType}'. Use one of: {VenueTypeNames}.");
        }

        var filters = new List<string>();
        var cleanName = CleanForGridify(name);
        if (cleanName.Length > 0)
        {
            filters.Add($"name=*{cleanName}");
        }

        if (type is not null)
        {
            filters.Add($"venueType={type}");
        }

        var query = new GridifyQuery
        {
            Page = 1,
            PageSize = MaxResults,
            Filter = filters.Count > 0 ? string.Join(",", filters) : null,
        };

        var page = await venues.GetVenuesAsync(query, includeOpeningHours: false, ct);

        return new
        {
            totalMatches = page.TotalCount,
            venues = page.Items.Select(v => new VenueSummary(v.Id, v.Name, v.VenueType.ToString(), v.Address)),
        };
    }

    [Description("Find venues near the user's current location, nearest first. Only works if the user shared their location.")]
    public async Task<object> FindNearbyVenues(
        [Description("Search radius in km. Default 5, max 50.")] double radiusKm = DefaultRadiusKm,
        [Description("Venue type: Rental, Cafe or Other. Leave empty for any type.")] string venueType = "",
        CancellationToken ct = default)
    {
        if (!context.HasLocation)
        {
            return Error("The user's location is unknown. Ask the user to share their location, or to search by venue name.");
        }

        if (!TryParseVenueType(venueType, out var type))
        {
            return Error($"Unknown venue type '{venueType}'. Use one of: {VenueTypeNames}.");
        }

        radiusKm = Math.Clamp(radiusKm, 0.5, MaxRadiusKm);

        var nearby = await venues.GetNearbyVenuesAsync(
            context.Latitude!.Value, context.Longitude!.Value, radiusKm * 1000, type, MaxResults, ct);

        // Say clearly what an empty list means. Otherwise the model tends to
        // guess (for example "I don't know your location") and fall back to
        // search_venues, which lists venues that are not nearby.
        if (nearby.Count == 0)
        {
            return new
            {
                radiusKm,
                venues = Array.Empty<NearbyVenueSummary>(),
                message = $"The user's location is known, but no {(type is null ? "venues" : type + " venues")} " +
                          $"are within {radiusKm} km. Tell the user and offer a bigger radius (max {MaxRadiusKm} km). " +
                          "Do not list venues from search_venues as if they were nearby.",
            };
        }

        return new
        {
            radiusKm,
            venues = nearby.Select(v => new NearbyVenueSummary(v.Id, v.Name, v.VenueType.ToString(), v.Address, v.DistanceKm)),
            message = (string?)null,
        };
    }

    [Description("Get details of one venue, including its weekly opening hours.")]
    public async Task<object> GetVenueDetails(
        [Description("The venue id from search_venues or find_nearby_venues.")] Guid venueId,
        CancellationToken ct = default)
    {
        var venue = await venues.GetVenueAsync(venueId, includeOpeningHours: true, ct);
        if (venue is null)
        {
            return Error("Venue not found.");
        }

        return new
        {
            venue.Id,
            venue.Name,
            venueType = venue.VenueType.ToString(),
            venue.Address,
            venue.Description,
            openingHours = (venue.OpeningHours ?? [])
                .Select(o => new { day = o.Day.ToString(), open = o.OpenTime.ToString("HH:mm"), close = o.CloseTime.ToString("HH:mm") }),
        };
    }

    [Description("List the bookable units of a venue, with price and minimum booking duration.")]
    public async Task<object> GetUnits(
        [Description("The venue id.")] Guid venueId,
        CancellationToken ct = default)
    {
        var page = await units.GetUnitsAsync(venueId, new GridifyQuery { Page = 1, PageSize = 50 }, ct);
        if (page is null)
        {
            return Error("Venue not found.");
        }

        return new
        {
            units = page.Items.Select(u => new UnitSummary(
                u.Id, u.Name, u.Price, u.Currency, u.MinBookingDurationMinutes, u.Description)),
        };
    }

    [Description("Check if a unit can be booked for a time range. Uses the same rules as a real booking: " +
                 "opening hours, minimum duration, same day, not in the past, and no overlap with other bookings.")]
    public async Task<object> CheckAvailability(
        [Description("The unit id from get_units.")] Guid unitId,
        [Description("Start time, venue local time, format yyyy-MM-ddTHH:mm.")] string startAt,
        [Description("End time, venue local time, format yyyy-MM-ddTHH:mm.")] string endAt,
        CancellationToken ct = default)
    {
        if (!TryParseLocalTime(startAt, out var start) || !TryParseLocalTime(endAt, out var end))
        {
            return Error("Times must use the format yyyy-MM-ddTHH:mm.");
        }

        if (end <= start)
        {
            return Error("The end time must be after the start time.");
        }

        var result = await bookings.CheckSlotAsync(unitId, start, end, ct);

        return result.Status switch
        {
            ResultStatus.Success => new { available = true, reason = (string?)null },
            ResultStatus.NotFound => Error("Unit not found."),
            _ => new { available = false, reason = result.Error },
        };
    }

    private static object Error(string message) => new { error = message };

    private static bool TryParseVenueType(string? value, out VenueType? type)
    {
        type = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (Enum.TryParse<VenueType>(value.Trim(), ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
        {
            type = parsed;
            return true;
        }

        return false;
    }

    // Takes the wall-clock time as written. If the model adds an offset anyway
    // (for example "+07:00" or "Z"), it is ignored instead of shifting the time.
    // Kind is set to Utc to match how the REST API receives times.
    private static bool TryParseLocalTime(string value, out DateTime time)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            time = DateTime.SpecifyKind(parsed.DateTime, DateTimeKind.Utc);
            return true;
        }

        time = default;
        return false;
    }

    // Removes characters that have a meaning in Gridify filter syntax, so text
    // from the model cannot change the filter.
    private static string CleanForGridify(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var kept = value.Trim().Where(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '\'' or '.' or '&');
        return new string(kept.Take(50).ToArray());
    }

    private sealed record VenueSummary(Guid Id, string Name, string VenueType, string? Address);

    private sealed record NearbyVenueSummary(Guid Id, string Name, string VenueType, string? Address, double DistanceKm);

    private sealed record UnitSummary(
        Guid Id, string Name, decimal? Price, string? Currency, int MinBookingDurationMinutes, string? Description);
}
