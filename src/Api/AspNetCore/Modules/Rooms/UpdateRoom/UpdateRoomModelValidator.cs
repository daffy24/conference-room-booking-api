using ConferenceBooking.Api.AspNetCore.Modules.Rooms.Validators;
using FluentValidation;

namespace ConferenceBooking.Api.AspNetCore.Modules.Rooms.UpdateRoom;

internal sealed class UpdateRoomModelValidator : AbstractValidator<UpdateRoomModel>
{
    public UpdateRoomModelValidator()
    {
        RuleFor(model => model).Must(model => model.Name is not null || model.Capacity.HasValue ||
            model.BaseHourlyRate.HasValue || model.Services is not null)
            .WithMessage("Supply at least one field to update.");
        RuleFor(model => model.Name).NotEmpty().MaximumLength(100).When(model => model.Name is not null);
        RuleFor(model => model.Capacity).GreaterThan(0).When(model => model.Capacity.HasValue);
        RuleFor(model => model.BaseHourlyRate).GreaterThan(0).LessThanOrEqualTo(1_000_000_000m)
            .PrecisionScale(18, 2, true).When(model => model.BaseHourlyRate.HasValue);
        RuleFor(model => model.Services!).SetValidator(new RoomServicesValidator())
            .When(model => model.Services is not null);
    }
}
