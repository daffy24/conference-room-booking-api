using ConferenceBooking.Core.Application.Modules.Rooms.Adapters;
using ConferenceBooking.Core.Application.Modules.Rooms.Models;
using ConferenceBooking.Core.Application.Modules.Rooms.Models.Requests;
using ConferenceBooking.Data;
using MediatR;

namespace ConferenceBooking.Core.Application.Modules.Rooms.Handlers;

internal sealed class AddRoomHandler(ConferenceBookingDbContext dbContext) : IRequestHandler<AddRoomRequest, RoomModel>
{
    public async Task<RoomModel> Handle(AddRoomRequest request, CancellationToken cancellationToken)
    {
        var room = request.ToEntity();
        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync(cancellationToken);
        return room.ToModel();
    }
}
