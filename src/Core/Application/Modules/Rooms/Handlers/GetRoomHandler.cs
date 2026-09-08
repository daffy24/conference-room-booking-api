using ConferenceBooking.Core.Application.Exceptions;
using ConferenceBooking.Core.Application.Modules.Rooms.Adapters;
using ConferenceBooking.Core.Application.Modules.Rooms.Models;
using ConferenceBooking.Core.Application.Modules.Rooms.Models.Requests;
using ConferenceBooking.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ConferenceBooking.Core.Application.Modules.Rooms.Handlers;

internal sealed class GetRoomHandler(ConferenceBookingDbContext dbContext) : IRequestHandler<GetRoomRequest, RoomModel>
{
    public async Task<RoomModel> Handle(GetRoomRequest request, CancellationToken cancellationToken)
    {
        var room = await dbContext.Rooms.AsNoTracking().Include(room => room.Services)
            .SingleOrDefaultAsync(room => room.Id == request.Id && !room.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Conference room was not found.");
        return room.ToModel();
    }
}
