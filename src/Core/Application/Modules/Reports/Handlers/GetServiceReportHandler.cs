using ConferenceBooking.Core.Application.Models;
using ConferenceBooking.Core.Application.Modules.Reports.Adapters;
using ConferenceBooking.Core.Application.Modules.Reports.Models;
using ConferenceBooking.Core.Application.Modules.Reports.Models.Requests;
using ConferenceBooking.Core.Application.Modules.Reports.Services;
using ConferenceBooking.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ConferenceBooking.Core.Application.Modules.Reports.Handlers;

internal sealed class GetServiceReportHandler(ConferenceBookingDbContext dbContext)
    : IRequestHandler<GetServiceReportRequest, PagedResult<ServiceReportModel>>
{
    public async Task<PagedResult<ServiceReportModel>> Handle(GetServiceReportRequest request, CancellationToken cancellationToken)
    {
        var period = new ReportPeriod(request.From, request.To);
        // Read booked snapshots: current service prices and catalogue deletions must not rewrite revenue history.
        var query = period.ApplyTo(dbContext.Bookings.AsNoTracking(), request.RoomId)
            .SelectMany(booking => booking.Services)
            .GroupBy(service => new { service.Booking.RoomId, RoomName = service.Booking.Room.Name, service.ServiceId })
            .Select(group => new ServiceReportRow
            {
                RoomId = group.Key.RoomId,
                RoomName = group.Key.RoomName,
                ServiceId = group.Key.ServiceId,
                ServiceName = group.OrderByDescending(service => service.Booking.CreatedAt)
                    .ThenByDescending(service => service.Booking.Id).Select(service => service.Name).First(),
                BookingCount = group.Count(),
                TotalRevenue = group.Sum(service => service.Price)
            });
        var count = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(row => row.BookingCount).ThenByDescending(row => row.TotalRevenue)
            .ThenBy(row => row.RoomId).ThenBy(row => row.ServiceId)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<ServiceReportModel>([.. rows.Select(row => row.ToModel())],
            count, request.Page, request.PageSize);
    }
}
