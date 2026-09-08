using ConferenceBooking.Core.Application.Models;
using MediatR;

namespace ConferenceBooking.Core.Application.Modules.Rooms.Models.Requests;

/// <summary>Finds rooms with sufficient capacity and no overlapping booking.</summary>
/// <param name="StartsAt">The inclusive start instant.</param>
/// <param name="EndsAt">The exclusive end instant.</param>
/// <param name="Capacity">The required number of seats.</param>
/// <param name="Page">The one-based page number.</param>
/// <param name="PageSize">The maximum results per page.</param>
public sealed record SearchAvailableRoomsRequest(DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    int Capacity, int Page = 1, int PageSize = 20) : IRequest<PagedResult<RoomModel>>;
