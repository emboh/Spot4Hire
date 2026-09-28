using FluentValidation;
using Spot4Hire.Backend.Dtos.Bookings;

namespace Spot4Hire.Backend.Validators.Bookings;

// Shared rules for create and update. UpdateBookingRequest inherits
// CreateBookingRequest, so both use this generic base.
// Only the basic time invariant is checked here. Availability rules
// (no overlap with existing bookings, respecting the unit's min duration
// and the venue's opening hours) belong in the service, not here.
public abstract class BookingRequestValidator<T> : AbstractValidator<T>
    where T : CreateBookingRequest
{
    protected BookingRequestValidator()
    {
        RuleFor(x => x.EndAt)
            .GreaterThan(x => x.StartAt)
            .WithMessage("End time must be after start time.");
    }
}

public sealed class CreateBookingRequestValidator : BookingRequestValidator<CreateBookingRequest> { }

public sealed class UpdateBookingRequestValidator : BookingRequestValidator<UpdateBookingRequest> { }
