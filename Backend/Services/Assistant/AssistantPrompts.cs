using System.Globalization;

namespace Spot4Hire.Backend.Services.Assistant;

public static class AssistantPrompts
{
    // Booking times and opening hours are stored as venue wall-clock times
    // (see BookingService.ValidateSlotAsync). All venues are in Indonesia for
    // now, so "now" is given to the model in this timezone.
    public const string VenueTimeZoneId = "Asia/Jakarta";

    public static string System(DateTimeOffset utcNow, bool hasUserLocation)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(VenueTimeZoneId);
        var localNow = TimeZoneInfo.ConvertTime(utcNow, zone)
            .ToString("dddd, yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        // The model never sees the coordinates, only whether they exist.
        // Without this line it may assume there is no location and skip
        // find_nearby_venues.
        var location = hasUserLocation
            ? "The user HAS shared their location. For questions like \"near me\" or \"nearby\", call find_nearby_venues."
            : "The user has NOT shared their location. For \"near me\" questions, ask them to share it or to give a venue name.";

        return $"""
            You are the assistant for Spot4Hire, an app for booking venues and their units
            (for example a court, a room or a table).

            Rules:
            - Only answer with facts you got from the tools. Never guess ids, prices,
              opening hours or availability.
            - If a question is not about Spot4Hire venues, units, opening hours or
              bookings, say that you can only help with Spot4Hire.
            - To find a venue by name, call search_venues first to get its id.
            - Before saying a unit is free, call check_availability.
            - When a tool returns an error, explain it to the user in simple words.
            - Keep answers short. Show prices with their currency.

            Location:
            - {location}

            Time:
            - Current local time: {localNow} ({VenueTimeZoneId}).
            - All times are venue local times. Pass them to tools as yyyy-MM-ddTHH:mm
              with no timezone suffix. Resolve words like "tomorrow" or "Saturday"
              from the current local time.
            """;
    }
}
