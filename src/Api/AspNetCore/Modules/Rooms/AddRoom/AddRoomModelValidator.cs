using ConferenceBooking.Api.AspNetCore.Modules.Rooms.Validators;
using FluentValidation;

namespace ConferenceBooking.Api.AspNetCore.Modules.Rooms.AddRoom;

internal sealed class AddRoomModelValidator : AbstractValidator<AddRoomModel>
{
    public AddRoomModelValidator()
    {
        RuleFor(model => model.Name).NotEmpty().MaximumLength(100);
        RuleFor(model => model.Capacity).GreaterThan(0);
        RuleFor(model => model.BaseHourlyRate).GreaterThan(0).LessThanOrEqualTo(1_000_000_000m).PrecisionScale(18, 2, true);
        RuleFor(model => model.Services).NotNull().SetValidator(new RoomServicesValidator())
            .DependentRules(() => RuleForEach(model => model.Services).Must(service => !service.Id.HasValue)
                .WithMessage("New services must not include existing identifiers."));
    }
}
