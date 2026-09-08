using ConferenceBooking.Core.Application.Exceptions;
using ConferenceBooking.Core.Application.Modules.Rooms.Models.Requests;
using ConferenceBooking.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ConferenceBooking.Core.Application.Modules.Rooms.Handlers;

internal sealed class DeleteRoomHandler(ConferenceBookingDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<DeleteRoomRequest>
{
    public async Task Handle(DeleteRoomRequest request, CancellationToken cancellationToken)
    {
        var room = await dbContext.Rooms.SingleOrDefaultAsync(room => room.Id == request.Id && !room.IsDeleted,
            cancellationToken) ?? throw new NotFoundException("Conference room was not found.");
        var now = timeProvider.GetUtcNow();
        if (await dbContext.Bookings.AnyAsync(booking => booking.RoomId == room.Id && booking.EndsAt > now,
                cancellationToken))
            throw new ConflictException("A room with an ongoing or future booking cannot be deleted.");

        // Preserve historical bookings; the version check also detects bookings created during deletion.
        room.IsDeleted = true;
        room.Version = Guid.NewGuid();
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
