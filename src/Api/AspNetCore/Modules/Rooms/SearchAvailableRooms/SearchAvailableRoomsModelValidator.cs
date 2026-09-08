using ConferenceBooking.Core.Application.Modules.Bookings.Services;
using FluentValidation;

namespace ConferenceBooking.Api.AspNetCore.Modules.Rooms.SearchAvailableRooms;

internal sealed class SearchAvailableRoomsModelValidator : AbstractValidator<SearchAvailableRoomsModel>
{
    public SearchAvailableRoomsModelValidator()
    {
        RuleFor(model => model).Must(model => BookingSchedule.IsValidPeriod(model.StartsAt, model.EndsAt))
            .WithMessage("Use whole minutes within one Kyiv calendar day, between 06:00 and 23:00, with end after start.");
        RuleFor(model => model.Capacity).GreaterThan(0);
        RuleFor(model => model.Page).InclusiveBetween(1, 1_000_000);
        RuleFor(model => model.PageSize).InclusiveBetween(1, 100);
    }
}
