using ConferenceBooking.Api.AspNetCore.Modules.Rooms.Models;
using FluentValidation;

namespace ConferenceBooking.Api.AspNetCore.Modules.Rooms.Validators;

internal sealed class RoomServicesValidator : AbstractValidator<IReadOnlyList<RoomServiceModel>>
{
    public RoomServicesValidator()
    {
        RuleFor(services => services.Count).LessThanOrEqualTo(20);
        // JSON collection elements can be null even when their C# type is non-nullable.
        // Validate elements before rules that dereference their properties.
        RuleForEach(services => services).NotNull().SetValidator(new RoomServiceModelValidator())
            .DependentRules(() =>
            {
                RuleFor(services => services).Must(HaveUniqueNames)
                    .WithMessage("Service names must be unique within a room.");
                RuleFor(services => services).Must(HaveUniqueIds)
                    .WithMessage("Service identifiers must not be repeated.");
            });
    }

    private static bool HaveUniqueNames(IReadOnlyList<RoomServiceModel> services)
        => services.Select(service => service.Name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == services.Count;

    private static bool HaveUniqueIds(IReadOnlyList<RoomServiceModel> services)
    {
        var ids = services.Where(service => service.Id.HasValue).Select(service => service.Id).ToArray();
        return ids.Distinct().Count() == ids.Length;
    }
}
