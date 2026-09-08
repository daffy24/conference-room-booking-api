using ConferenceBooking.Core.Application.Exceptions;
using ConferenceBooking.Core.Application.Modules.Bookings.Adapters;
using ConferenceBooking.Core.Application.Modules.Bookings.Models;
using ConferenceBooking.Core.Application.Modules.Bookings.Models.Requests;
using ConferenceBooking.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ConferenceBooking.Core.Application.Modules.Bookings.Handlers;

internal sealed class GetBookingHandler(ConferenceBookingDbContext dbContext)
    : IRequestHandler<GetBookingRequest, BookingModel>
{
    public async Task<BookingModel> Handle(GetBookingRequest request, CancellationToken cancellationToken)
    {
        // Filter by ownership in SQL so unrelated callers cannot distinguish hidden bookings from missing ones.
        var booking = await dbContext.Bookings.AsNoTracking().Include(booking => booking.Services)
            .SingleOrDefaultAsync(booking => booking.Id == request.Id &&
                (request.IsAdmin || booking.UserId == request.UserId), cancellationToken)
            ?? throw new NotFoundException("Booking was not found.");
        return booking.ToModel();
    }
}
