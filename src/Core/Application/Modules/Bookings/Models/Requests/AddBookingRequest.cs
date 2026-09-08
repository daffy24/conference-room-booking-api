using MediatR;

namespace ConferenceBooking.Core.Application.Modules.Bookings.Models.Requests;

/// <summary>Books a room for the authenticated caller.</summary>
/// <param name="UserId">The subject from a validated access token.</param>
/// <param name="RoomId">The selected room.</param>
/// <param name="StartsAt">The start instant with an explicit UTC offset.</param>
/// <param name="DurationMinutes">The positive booking duration in whole minutes.</param>
/// <param name="ServiceIds">The selected services offered by the room.</param>
public sealed record AddBookingRequest(string UserId, Guid RoomId, DateTimeOffset StartsAt, int DurationMinutes,
    IReadOnlyList<Guid> ServiceIds) : IRequest<BookingModel>;
