using Microsoft.EntityFrameworkCore;
using Spot4Hire.Backend.Common;
using Spot4Hire.Backend.Data.Entities;
using Spot4Hire.Backend.Domain;
using Spot4Hire.Backend.Dtos.Bookings;
using Spot4Hire.Backend.Services;
using Spot4Hire.Backend.Tests.Infrastructure;
using Xunit;

namespace Spot4Hire.Backend.Tests.Bookings;

[Collection(nameof(DatabaseCollection))]
public sealed class BookingServiceTests(SqlServerFixture fixture) : IAsyncLifetime
{
    // A fixed future weekday and time so the "past" and opening-hours checks are stable.
    private static readonly DateTime SlotStart = NextMonday(new TimeOnly(10, 0));
    private static readonly DateTime SlotEnd = NextMonday(new TimeOnly(11, 0));

    private Guid _unitId;
    private Guid _userId;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        await SeedAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Create_valid_booking_succeeds()
    {
        await using var db = fixture.CreateContext();
        var service = new BookingService(db);

        var result = await service.CreateBookingAsync(_unitId, _userId,
            new CreateBookingRequest { StartAt = SlotStart, EndAt = SlotEnd }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(_userId, result.Value!.UserId);
    }

    [Fact]
    public async Task Create_in_the_past_is_invalid()
    {
        await using var db = fixture.CreateContext();
        var service = new BookingService(db);

        var start = DateTime.UtcNow.AddHours(-2);
        var result = await service.CreateBookingAsync(_unitId, _userId,
            new CreateBookingRequest { StartAt = start, EndAt = start.AddHours(1) }, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Create_shorter_than_min_duration_is_invalid()
    {
        await using var db = fixture.CreateContext();
        var service = new BookingService(db);

        // Unit min duration is 1 hour; ask for 30 minutes.
        var result = await service.CreateBookingAsync(_unitId, _userId,
            new CreateBookingRequest { StartAt = SlotStart, EndAt = SlotStart.AddMinutes(30) }, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Create_outside_opening_hours_is_invalid()
    {
        await using var db = fixture.CreateContext();
        var service = new BookingService(db);

        // Opening hours are 08:00-22:00; 06:00 is before open.
        var start = NextMonday(new TimeOnly(6, 0));
        var result = await service.CreateBookingAsync(_unitId, _userId,
            new CreateBookingRequest { StartAt = start, EndAt = start.AddHours(1) }, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Create_overlapping_booking_is_conflict()
    {
        await using (var db = fixture.CreateContext())
        {
            var result = await new BookingService(db).CreateBookingAsync(_unitId, _userId,
                new CreateBookingRequest { StartAt = SlotStart, EndAt = SlotEnd }, CancellationToken.None);
            Assert.True(result.IsSuccess);
        }

        await using (var db = fixture.CreateContext())
        {
            var overlap = await new BookingService(db).CreateBookingAsync(_unitId, _userId,
                new CreateBookingRequest { StartAt = SlotStart.AddMinutes(30), EndAt = SlotEnd.AddMinutes(30) }, CancellationToken.None);
            Assert.Equal(ResultStatus.Conflict, overlap.Status);
        }
    }

    [Fact]
    public async Task Concurrent_bookings_for_same_slot_only_one_wins()
    {
        var request = new CreateBookingRequest { StartAt = SlotStart, EndAt = SlotEnd };

        async Task<Result<BookingResponse>> Book()
        {
            await using var db = fixture.CreateContext();
            return await new BookingService(db).CreateBookingAsync(_unitId, _userId, request, CancellationToken.None);
        }

        var results = await Task.WhenAll(Book(), Book());

        Assert.Equal(1, results.Count(r => r.IsSuccess));

        await using var check = fixture.CreateContext();
        Assert.Equal(1, await check.Bookings.CountAsync(b => b.UnitId == _unitId));
    }

    [Fact]
    public async Task Update_by_non_owner_is_forbidden()
    {
        Guid bookingId;
        await using (var db = fixture.CreateContext())
        {
            var created = await new BookingService(db).CreateBookingAsync(_unitId, _userId,
                new CreateBookingRequest { StartAt = SlotStart, EndAt = SlotEnd }, CancellationToken.None);
            bookingId = created.Value!.Id;
        }

        await using (var db = fixture.CreateContext())
        {
            var result = await new BookingService(db).UpdateBookingAsync(
                _unitId, bookingId, Guid.NewGuid(), isAdmin: false,
                new UpdateBookingRequest { StartAt = SlotStart, EndAt = SlotEnd }, CancellationToken.None);

            Assert.Equal(ResultStatus.Forbidden, result.Status);
        }
    }

    [Fact]
    public async Task Admin_can_delete_another_users_booking()
    {
        Guid bookingId;
        await using (var db = fixture.CreateContext())
        {
            var created = await new BookingService(db).CreateBookingAsync(_unitId, _userId,
                new CreateBookingRequest { StartAt = SlotStart, EndAt = SlotEnd }, CancellationToken.None);
            bookingId = created.Value!.Id;
        }

        await using (var db = fixture.CreateContext())
        {
            var result = await new BookingService(db)
                .DeleteBookingAsync(_unitId, bookingId, Guid.NewGuid(), isAdmin: true, CancellationToken.None);
            Assert.True(result.IsSuccess);
        }
    }

    private async Task SeedAsync()
    {
        await using var db = fixture.CreateContext();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "customer@test.local",
            Email = "customer@test.local",
        };
        var venue = new Venue { Id = Guid.NewGuid(), Name = "Test Venue", VenueType = VenueType.Rental };
        var unit = new Unit
        {
            Id = Guid.NewGuid(),
            VenueId = venue.Id,
            Name = "Room A",
            MinBookingDuration = TimeSpan.FromHours(1),
        };
        var hours = new OpeningHour
        {
            Id = Guid.NewGuid(),
            VenueId = venue.Id,
            Day = SlotStart.DayOfWeek,
            OpenTime = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(22, 0),
        };

        db.Users.Add(user);
        db.Venues.Add(venue);
        db.Units.Add(unit);
        db.OpeningHours.Add(hours);
        await db.SaveChangesAsync();

        _userId = user.Id;
        _unitId = unit.Id;
    }

    private static DateTime NextMonday(TimeOnly time)
    {
        var date = DateTime.UtcNow.Date.AddDays(1);
        while (date.DayOfWeek != DayOfWeek.Monday)
        {
            date = date.AddDays(1);
        }

        return date + time.ToTimeSpan();
    }
}