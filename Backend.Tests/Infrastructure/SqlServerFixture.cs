using Microsoft.EntityFrameworkCore;
using Spot4Hire.Backend.Data;
using Testcontainers.MsSql;
using Xunit;

namespace Spot4Hire.Backend.Tests.Infrastructure;

// Starts one SQL Server container for the whole test collection.
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder().Build();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        // Build the schema once from the EF model (no migrations needed here).
        await using var db = CreateContext();
        await db.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_container.GetConnectionString(), sql =>
            {
                sql.UseNetTopologySuite();
                // Lets the retry strategy re-run the deadlock loser so it returns
                // a clean Conflict instead of throwing (matches production).
                sql.EnableRetryOnFailure();
            })
            .Options;

        return new AppDbContext(options);
    }

    // Clears rows between tests, children before parents to respect foreign keys.
    public async Task ResetAsync()
    {
        await using var db = CreateContext();
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Bookings");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM OpeningHours");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Units");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Venues");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM AspNetUsers");
    }
}