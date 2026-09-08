using ConferenceBooking.Core.Application.Models;
using ConferenceBooking.Core.Application.Modules.Reports.Adapters;
using ConferenceBooking.Core.Application.Modules.Reports.Models;
using ConferenceBooking.Core.Application.Modules.Reports.Models.Requests;
using ConferenceBooking.Core.Application.Modules.Reports.Services;
using ConferenceBooking.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ConferenceBooking.Core.Application.Modules.Reports.Handlers;

internal sealed class GetRoomReportHandler(ConferenceBookingDbContext dbContext)
    : IRequestHandler<GetRoomReportRequest, PagedResult<RoomReportModel>>
{
    public async Task<PagedResult<RoomReportModel>> Handle(GetRoomReportRequest request, CancellationToken cancellationToken)
    {
        var period = new ReportPeriod(request.From, request.To);
        var totals = period.ApplyTo(dbContext.Bookings.AsNoTracking(), request.RoomId)
            .GroupBy(booking => booking.RoomId)
            .Select(group => new
            {
                RoomId = group.Key,
                BookingCount = (int?)group.Count(),
                BookedMinutes = (decimal?)group.Sum(booking => (decimal)(booking.EndsAt - booking.StartsAt).TotalMinutes),
                RentalRevenue = (decimal?)group.Sum(booking => booking.RentalCost),
                ServicesRevenue = (decimal?)group.Sum(booking => booking.ServicesCost),
                TotalRevenue = (decimal?)group.Sum(booking => booking.TotalCost)
            });
        var rooms = dbContext.Rooms.AsNoTracking();
        if (request.RoomId.HasValue)
        {
            rooms = rooms.Where(room => room.Id == request.RoomId.Value);
        }

        // Keep active rooms with no demand visible; deleted rooms remain only when they have matching history.
        var query = from room in rooms
                    join total in totals on room.Id equals total.RoomId into roomTotals
                    from total in roomTotals.DefaultIfEmpty()
                    where !room.IsDeleted || total.BookingCount.HasValue
                    select new RoomReportRow
                    {
                        RoomId = room.Id,
                        RoomName = room.Name,
                        IsDeleted = room.IsDeleted,
                        BookingCount = total.BookingCount ?? 0,
                        BookedMinutes = total.BookedMinutes ?? 0,
                        RentalRevenue = total.RentalRevenue ?? 0,
                        ServicesRevenue = total.ServicesRevenue ?? 0,
                        TotalRevenue = total.TotalRevenue ?? 0
                    };
        var count = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(row => row.TotalRevenue).ThenBy(row => row.RoomId)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<RoomReportModel>([.. rows.Select(row => row.ToModel())],
            count, request.Page, request.PageSize);
    }
}
