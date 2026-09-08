using ConferenceBooking.Core.Application.Modules.Bookings.Services;
using FluentValidation;

namespace ConferenceBooking.Api.AspNetCore.Modules.Bookings.AddBooking;

internal sealed class AddBookingModelValidator : AbstractValidator<AddBookingModel>
{
    public AddBookingModelValidator(TimeProvider timeProvider)
    {
        RuleFor(model => model.RoomId).NotEmpty();
        RuleFor(model => model.StartsAt).Must(start => start > timeProvider.GetUtcNow())
            .WithMessage("Bookings must start in the future.");
        RuleFor(model => model.DurationMinutes).InclusiveBetween(1, 17 * 60);
        RuleFor(model => model).Must(HasValidPeriod)
            .WithMessage("Use whole minutes within one Kyiv calendar day, between 06:00 and 23:00.");
        RuleFor(model => model.ServiceIds).Cascade(CascadeMode.Stop).NotNull()
            .Must(ids => ids.Count <= 20).WithMessage("Select at most 20 services.")
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Service identifiers must be unique.");
        RuleForEach(model => model.ServiceIds).NotEmpty();
    }

    private static bool HasValidPeriod(AddBookingModel model)
    {
        if (model.DurationMinutes is < 1 or > 17 * 60)
            return false;

        try
        {
            return BookingSchedule.IsValidPeriod(model.StartsAt, model.StartsAt.AddMinutes(model.DurationMinutes));
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}
