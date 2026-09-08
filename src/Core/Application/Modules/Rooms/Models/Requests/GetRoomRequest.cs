using MediatR;

namespace ConferenceBooking.Core.Application.Modules.Rooms.Models.Requests;

/// <summary>Retrieves an active conference room.</summary>
/// <param name="Id">The room identifier.</param>
public sealed record GetRoomRequest(Guid Id) : IRequest<RoomModel>;
