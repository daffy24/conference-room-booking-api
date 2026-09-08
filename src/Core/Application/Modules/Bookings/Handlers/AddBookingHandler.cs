using ConferenceBooking.Core.Application.Exceptions;
using ConferenceBooking.Core.Application.Modules.Bookings.Adapters;
using ConferenceBooking.Core.Application.Modules.Bookings.Models;
using ConferenceBooking.Core.Application.Modules.Bookings.Models.Requests;
using ConferenceBooking.Core.Application.Modules.Bookings.Services;
using ConferenceBooking.Data;
using ConferenceBooking.Data.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ConferenceBooking.Core.Application.Modules.Bookings.Handlers;

internal sealed class AddBookingHandler(ConferenceBookingDbContext dbContext,
    RentalPriceCalculator priceCalculator, TimeProvider timeProvider) : IRequestHandler<AddBookingRequest, BookingModel>
{
    public async Task<BookingModel> Handle(AddBookingRequest request, CancellationToken cancellationToken)
    {
        var room = await dbContext.Rooms.Include(room => room.Services)
            .SingleOrDefaultAsync(room => room.Id == request.RoomId && !room.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Conference room was not found.");
        var startsAt = request.StartsAt.ToUniversalTime();
        var endsAt = startsAt.AddMinutes(request.DurationMinutes);
        if (await dbContext.Bookings.AnyAsync(booking => booking.RoomId == room.Id &&
                booking.StartsAt < endsAt && booking.EndsAt > startsAt, cancellationToken))
            throw new ConflictException("The room is already booked for part of the requested period.");

        var selectedServices = room.Services.Where(service => request.ServiceIds.Contains(service.Id)).ToArray();
        if (selectedServices.Length != request.ServiceIds.Count)
            throw new ValidationException([new ValidationFailure("ServiceIds", "Select only services currently offered by this room.")]);

        var rentalCost = priceCalculator.Calculate(room.BaseHourlyRate, startsAt, endsAt);
        var servicesCost = selectedServices.Sum(service => service.Price);
        var booking = new BookingEntity
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            Room = room,
            UserId = request.UserId,
            RoomName = room.Name,
            BaseHourlyRate = room.BaseHourlyRate,
            StartsAt = startsAt,
            EndsAt = endsAt,
            CreatedAt = timeProvider.GetUtcNow(),
            RentalCost = rentalCost,
            ServicesCost = servicesCost,
            TotalCost = rentalCost + servicesCost,
            Services = [.. selectedServices.Select(service => service.ToBookingService())],
        };

        // The room token coordinates booking with edits/deletion. PostgreSQL additionally rejects overlaps.
        // SaveChanges commits the token, booking and price snapshots in one transaction.
        room.Version = Guid.NewGuid();
        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync(cancellationToken);
        return booking.ToModel();
    }
}
