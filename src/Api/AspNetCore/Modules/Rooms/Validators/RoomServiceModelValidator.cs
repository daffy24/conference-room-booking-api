using ConferenceBooking.Api.AspNetCore.Modules.Rooms.Models;
using FluentValidation;

namespace ConferenceBooking.Api.AspNetCore.Modules.Rooms.Validators;

internal sealed class RoomServiceModelValidator : AbstractValidator<RoomServiceModel>
{
    public RoomServiceModelValidator()
    {
        RuleFor(model => model.Id).NotEqual(Guid.Empty).When(model => model.Id.HasValue);
        RuleFor(model => model.Name).NotEmpty().MaximumLength(100);
        RuleFor(model => model.Price).InclusiveBetween(0, 1_000_000_000m).PrecisionScale(18, 2, true);
    }
}
