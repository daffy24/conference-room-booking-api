using ConferenceBooking.Core.Application.Models;
using ConferenceBooking.Core.Application.Modules.Rooms.Adapters;
using ConferenceBooking.Core.Application.Modules.Rooms.Models;
using ConferenceBooking.Core.Application.Modules.Rooms.Models.Requests;
using ConferenceBooking.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ConferenceBooking.Core.Application.Modules.Rooms.Handlers;

internal sealed class SearchAvailableRoomsHandler(ConferenceBookingDbContext dbContext)
    : IRequestHandler<SearchAvailableRoomsRequest, PagedResult<RoomModel>>
{
    public async Task<PagedResult<RoomModel>> Handle(SearchAvailableRoomsRequest request, CancellationToken cancellationToken)
    {
        var startsAt = request.StartsAt.ToUniversalTime();
        var endsAt = request.EndsAt.ToUniversalTime();
        // Half-open intervals allow one booking to start exactly when another ends.
        var query = dbContext.Rooms.AsNoTracking().Where(room => !room.IsDeleted && room.Capacity >= request.Capacity)
            .Where(room => !dbContext.Bookings.Any(booking => booking.RoomId == room.Id &&
                booking.StartsAt < endsAt && booking.EndsAt > startsAt));
        var count = await query.CountAsync(cancellationToken);
        var rooms = await query.OrderBy(room => room.Capacity).ThenBy(room => room.Id)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Include(room => room.Services).ToListAsync(cancellationToken);

        return new PagedResult<RoomModel>([.. rooms.Select(room => room.ToModel())], count, request.Page, request.PageSize);
    }
}
