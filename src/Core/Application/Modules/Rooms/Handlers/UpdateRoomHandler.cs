using ConferenceBooking.Core.Application.Exceptions;
using ConferenceBooking.Core.Application.Modules.Rooms.Adapters;
using ConferenceBooking.Core.Application.Modules.Rooms.Models;
using ConferenceBooking.Core.Application.Modules.Rooms.Models.Requests;
using ConferenceBooking.Data;
using ConferenceBooking.Data.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ConferenceBooking.Core.Application.Modules.Rooms.Handlers;

internal sealed class UpdateRoomHandler(ConferenceBookingDbContext dbContext) : IRequestHandler<UpdateRoomRequest, RoomModel>
{
    public async Task<RoomModel> Handle(UpdateRoomRequest request, CancellationToken cancellationToken)
    {
        var room = await dbContext.Rooms.Include(room => room.Services)
            .SingleOrDefaultAsync(room => room.Id == request.Id && !room.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Conference room was not found.");

        if (request.Name is not null) room.Name = request.Name.Trim();
        if (request.Capacity.HasValue) room.Capacity = request.Capacity.Value;
        if (request.BaseHourlyRate.HasValue) room.BaseHourlyRate = request.BaseHourlyRate.Value;
        if (request.Services is not null) ReplaceServices(room, request.Services);

        // Booking also updates this token, so an edit cannot silently change a concurrent quote.
        room.Version = Guid.NewGuid();
        await dbContext.SaveChangesAsync(cancellationToken);
        return room.ToModel();
    }

    private void ReplaceServices(RoomEntity room, IReadOnlyList<RoomServiceInput> requestedServices)
    {
        var existingById = room.Services.ToDictionary(service => service.Id);
        var existingByName = room.Services.ToDictionary(service => service.Name, StringComparer.OrdinalIgnoreCase);
        var retained = new HashSet<Guid>();
        foreach (var input in requestedServices)
        {
            // Match the original names so a rename earlier in this request cannot change another match.
            var service = input.Id.HasValue
                ? existingById.GetValueOrDefault(input.Id.Value)
                : existingByName.GetValueOrDefault(input.Name.Trim());
            if (input.Id.HasValue && service is null)
                throw new ValidationException([new ValidationFailure("Services", "A service does not belong to this room.")]);
            if (service is null)
            {
                service = new RoomServiceEntity { Id = Guid.NewGuid(), RoomId = room.Id, Room = room };
                room.Services.Add(service);
            }
            if (!retained.Add(service.Id))
                throw new ValidationException([new ValidationFailure("Services", "Each existing service may appear only once.")]);
            service.Name = input.Name.Trim();
            service.Price = input.Price;
        }

        foreach (var removed in room.Services.Where(service => !retained.Contains(service.Id)).ToArray())
        {
            room.Services.Remove(removed);
            dbContext.RoomServices.Remove(removed);
        }
    }
}
