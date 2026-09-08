using MediatR;

namespace ConferenceBooking.Core.Application.Modules.Rooms.Models.Requests;

/// <summary>Updates only the supplied room fields.</summary>
/// <param name="Id">The room identifier.</param>
/// <param name="Name">A replacement name, or null to retain it.</param>
/// <param name="Capacity">A replacement capacity, or null to retain it.</param>
/// <param name="BaseHourlyRate">A replacement rate, or null to retain it.</param>
/// <param name="Services">The complete service list, or null to retain the current list.</param>
public sealed record UpdateRoomRequest(Guid Id, string? Name, int? Capacity, decimal? BaseHourlyRate,
    IReadOnlyList<RoomServiceInput>? Services) : IRequest<RoomModel>;
