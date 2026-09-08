using MediatR;

namespace ConferenceBooking.Core.Application.Modules.Rooms.Models.Requests;

/// <summary>Removes a room from the catalogue while preserving booking history.</summary>
/// <param name="Id">The room identifier.</param>
public sealed record DeleteRoomRequest(Guid Id) : IRequest;
