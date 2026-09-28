using System.Data;
using Microsoft.EntityFrameworkCore;
using Spot4Hire.Backend.Common;
using Spot4Hire.Backend.Data;
using Spot4Hire.Backend.Data.Entities;
using Spot4Hire.Backend.Dtos.Bookings;
using Spot4Hire.Backend.Dtos.Common;
using Spot4Hire.Backend.Mapping;
using Spot4Hire.Backend.Services.Abstractions;
using Gridify;

namespace Spot4Hire.Backend.Services;

// Booking is a child of Unit. Reads are scoped to the unit; writes enforce the
// booking rules (see ValidateSlotAsync) and prevent double-booking.
public class BookingService(AppDbContext db) : IBookingService
{
    public async Task<PagedResponse<BookingResponse>?> GetBookingsAsync(Guid unitId, GridifyQuery query, CancellationToken ct)
    {
        if (!await db.Units.AnyAsync(u => u.Id == unitId, ct))
        {
            return null;
        }

        return await db.Bookings.AsNoTracking()
            .Where(b => b.UnitId == unitId)
            .ToPagedResponseAsync(query, GridifyMappers.Booking, b => b.ToResponse(), "startAt", ct);
    }

    public async Task<BookingResponse?> GetBookingAsync(Guid unitId, Guid id, CancellationToken ct)
    {
        var booking = await db.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id && b.UnitId == unitId, ct);

        return booking?.ToResponse();
    }

    public async Task<Result<BookingResponse>> CreateBookingAsync(Guid unitId, Guid userId, CreateBookingRequest request, CancellationToken ct)
    {
        var unit = await db.Units.AsNoTracking().FirstOrDefaultAsync(u => u.Id == unitId, ct);
        if (unit is null)
        {
            return Result<BookingResponse>.NotFound();
        }

        var slot = await ValidateSlotAsync(unit, request.StartAt, request.EndAt, ct);
        if (!slot.IsSuccess)
        {
            return Result<BookingResponse>.Invalid(slot.Error!);
        }

        // The overlap check and the insert must be atomic, or two concurrent
        // requests could both pass the check and double-book. Serializable
        // isolation holds a range lock so the second one waits, then sees the
        // committed booking and is rejected.
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            if (await HasOverlapAsync(unitId, request.StartAt, request.EndAt, excludeBookingId: null, ct))
            {
                return Result<BookingResponse>.Conflict("That time slot is already booked.");
            }

            var booking = request.ToEntity();
            booking.UnitId = unitId;
            booking.UserId = userId;

            db.Bookings.Add(booking);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Result<BookingResponse>.Success(booking.ToResponse());
        });
    }

    public async Task<Result> UpdateBookingAsync(Guid unitId, Guid id, Guid userId, bool isAdmin, UpdateBookingRequest request, CancellationToken ct)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.UnitId == unitId, ct);
        if (booking is null)
        {
            return Result.NotFound();
        }

        if (booking.UserId != userId && !isAdmin)
        {
            return Result.Forbidden("You can only change your own bookings.");
        }

        var unit = await db.Units.AsNoTracking().FirstOrDefaultAsync(u => u.Id == unitId, ct);
        if (unit is null)
        {
            return Result.NotFound();
        }

        var slot = await ValidateSlotAsync(unit, request.StartAt, request.EndAt, ct);
        if (!slot.IsSuccess)
        {
            return slot;
        }

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            if (await HasOverlapAsync(unitId, request.StartAt, request.EndAt, excludeBookingId: id, ct))
            {
                return Result.Conflict("That time slot is already booked.");
            }

            request.ApplyTo(booking);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Result.Success();
        });
    }

    public async Task<Result> DeleteBookingAsync(Guid unitId, Guid id, Guid userId, bool isAdmin, CancellationToken ct)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.UnitId == unitId, ct);
        if (booking is null)
        {
            return Result.NotFound();
        }

        if (booking.UserId != userId && !isAdmin)
        {
            return Result.Forbidden("You can only cancel your own bookings.");
        }

        db.Bookings.Remove(booking);
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }

    // Stateless-plus-opening-hours checks. Assumes booking times and opening
    // hours share one timezone (treated as UTC here); real multi-timezone
    // handling would convert before comparing.
    private async Task<Result> ValidateSlotAsync(Unit unit, DateTime startAt, DateTime endAt, CancellationToken ct)
    {
        if (startAt < DateTime.UtcNow)
        {
            return Result.Invalid("Booking cannot start in the past.");
        }

        if (endAt - startAt < unit.MinBookingDuration)
        {
            return Result.Invalid($"Booking must be at least {unit.MinBookingDuration.TotalMinutes:0} minutes long.");
        }

        if (startAt.Date != endAt.Date)
        {
            return Result.Invalid("Booking must start and end on the same day.");
        }

        var day = startAt.DayOfWeek;
        var start = TimeOnly.FromDateTime(startAt);
        var end = TimeOnly.FromDateTime(endAt);

        var windows = await db.OpeningHours
            .AsNoTracking()
            .Where(o => o.VenueId == unit.VenueId && o.Day == day)
            .ToListAsync(ct);

        if (windows.Count == 0)
        {
            return Result.Invalid("The venue is closed on that day.");
        }

        if (!windows.Any(w => w.OpenTime <= start && end <= w.CloseTime))
        {
            return Result.Invalid("Booking is outside the venue's opening hours.");
        }

        return Result.Success();
    }

    // Two ranges overlap when each starts before the other ends.
    private Task<bool> HasOverlapAsync(Guid unitId, DateTime startAt, DateTime endAt, Guid? excludeBookingId, CancellationToken ct)
        => db.Bookings.AnyAsync(
            b => b.UnitId == unitId
                 && (excludeBookingId == null || b.Id != excludeBookingId)
                 && b.StartAt < endAt
                 && startAt < b.EndAt,
            ct);

    public async Task<Result> CheckSlotAsync(Guid unitId, DateTime startAt, DateTime endAt, CancellationToken ct)
    {
        var unit = await db.Units.AsNoTracking().FirstOrDefaultAsync(u => u.Id == unitId, ct);
        if (unit is null)
        {
            return Result.NotFound();
        }

        var slot = await ValidateSlotAsync(unit, startAt, endAt, ct);
        if (!slot.IsSuccess)
        {
            return slot;
        }

        return await HasOverlapAsync(unitId, startAt, endAt, excludeBookingId: null, ct)
            ? Result.Conflict("That time slot is already booked.")
            : Result.Success();
    }
}
