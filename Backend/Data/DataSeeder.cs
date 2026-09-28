using System.Globalization;
using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Spot4Hire.Backend.Authorization;
using Spot4Hire.Backend.Data.Entities;
using Spot4Hire.Backend.Domain;

namespace Spot4Hire.Backend.Data;

// Development-only sample data. Fills Venues (with Units and OpeningHours) and a
// few Identity users. Bookings are intentionally left empty. Idempotent: it skips
// a table that already has rows, so it is safe to run on every startup.
public static class DataSeeder
{
    private const int Wgs84 = 4326;
    private const string DevPassword = "Password123!";
    private const string AdminEmail = "admin@spot4hire.local";

    // Bogus locale for the sample venues and units. The unit currency is taken
    // from the same locale, so changing this one value keeps them consistent.
    private const string Locale = "id_ID";

    // Venues are placed inside a box around Surabaya, so "near me" searches
    // (find_nearby_venues, GET api/venues/nearby) return results in dev.
    private const double MinLatitude = -7.35, MaxLatitude = -7.20;
    private const double MinLongitude = 112.60, MaxLongitude = 112.82;

    public static async Task SeedAsync(IServiceProvider services)
    {
        // One fixed seed so repeated runs produce the same data.
        Randomizer.Seed = new Random(4711);

        await SeedUsersAsync(services);

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var ownerIds = (await userManager.GetUsersInRoleAsync(Roles.Owner))
            .Select(o => o.Id)
            .ToList();

        await SeedVenuesAsync(services.GetRequiredService<AppDbContext>(), ownerIds);
    }

    private static async Task SeedVenuesAsync(AppDbContext db, IReadOnlyList<Guid> ownerIds)
    {
        if (ownerIds.Count == 0 || await db.Venues.AnyAsync())
        {
            return;
        }

        var currency = CurrencyFor(Locale);

        var unitFaker = new Faker<Unit>(Locale)
            .RuleFor(u => u.Name, f => f.Commerce.ProductName().Truncate(50))
            .RuleFor(u => u.Price, f => Math.Round(f.Random.Decimal(50_000, 500_000), 2))
            .RuleFor(u => u.Currency, _ => currency)
            .RuleFor(u => u.Description, f => f.Lorem.Sentence().Truncate(200))
            .RuleFor(u => u.MinBookingDuration, f => TimeSpan.FromMinutes(f.PickRandom(30, 60, 90, 120)));

        // Street addresses and company names follow Locale (Indonesian, to match
        // the Surabaya coordinates). Missing locale data falls back to English.
        var venueFaker = new Faker<Venue>(Locale)
            .RuleFor(v => v.UserId, f => ownerIds[f.Random.Int(0, ownerIds.Count - 1)])
            .RuleFor(v => v.Name, f => f.Company.CompanyName().Truncate(50))
            .RuleFor(v => v.VenueType, f => f.PickRandom<VenueType>())
            .RuleFor(v => v.Address, f => f.Address.FullAddress().Truncate(250))
            .RuleFor(v => v.Description, f => f.Lorem.Sentence().Truncate(200))
            .RuleFor(v => v.Coordinates, f => f.Random.Bool(0.7f)
                ? new Point(
                    f.Address.Longitude(MinLongitude, MaxLongitude),
                    f.Address.Latitude(MinLatitude, MaxLatitude)) { SRID = Wgs84 }
                : null)
            .RuleFor(v => v.Units, (f, _) => unitFaker.Generate(f.Random.Int(1, 5)))
            .RuleFor(v => v.OpeningHours, (f, _) => BuildOpeningHours(f));

        var venues = venueFaker.Generate(15);

        db.Venues.AddRange(venues);
        await db.SaveChangesAsync();
    }

    // ISO 4217 code for a Bogus locale: "id_ID" -> culture "id-ID" -> region ID -> "IDR".
    private static string CurrencyFor(string bogusLocale)
        => new RegionInfo(bogusLocale.Replace('_', '-')).ISOCurrencySymbol;

    // One window per day, Monday to Saturday (closed Sunday), always Close after Open.
    private static List<OpeningHour> BuildOpeningHours(Faker f)
    {
        var days = new[]
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday,
        };

        return days.Select(day => new OpeningHour
        {
            Day = day,
            OpenTime = new TimeOnly(f.PickRandom(7, 8, 9), 0),
            CloseTime = new TimeOnly(f.PickRandom(20, 21, 22), 0),
        }).ToList();
    }

    private static async Task SeedUsersAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        if (await userManager.Users.AnyAsync())
        {
            return;
        }

        var userFaker = new Faker<ApplicationUser>()
            .RuleFor(u => u.UserName, f => f.Internet.Email())
            .RuleFor(u => u.Email, (_, u) => u.UserName)
            .RuleFor(u => u.EmailConfirmed, _ => true)
            .RuleFor(u => u.Address, f => f.Address.FullAddress().Truncate(250));

        foreach (var customer in userFaker.Generate(10))
        {
            await CreateUserAsync(userManager, customer, Roles.Customer);
        }

        foreach (var owner in userFaker.Generate(10))
        {
            await CreateUserAsync(userManager, owner, Roles.Owner);
        }

        // A fixed superadmin so there is always a known login.
        var admin = new ApplicationUser
        {
            UserName = AdminEmail,
            Email = AdminEmail,
            EmailConfirmed = true,
        };
        await CreateUserAsync(userManager, admin, Roles.Admin);
    }

    private static async Task CreateUserAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, string role)
    {
        var result = await userManager.CreateAsync(user, DevPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }
}

file static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
