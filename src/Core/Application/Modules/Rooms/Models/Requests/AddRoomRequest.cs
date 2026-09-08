using MediatR;

namespace ConferenceBooking.Core.Application.Modules.Rooms.Models.Requests;

/// <summary>Creates a conference room.</summary>
/// <param name="Name">The room name.</param>
/// <param name="Capacity">The maximum attendance.</param>
/// <param name="BaseHourlyRate">The standard hourly rate in UAH.</param>
/// <param name="Services">The available services.</param>
public sealed record AddRoomRequest(string Name, int Capacity, decimal BaseHourlyRate,
    IReadOnlyList<RoomServiceInput> Services) : IRequest<RoomModel>;
