using ConferenceBooking.Core.Application.Modules.Reports.Models;
using ConferenceBooking.Core.Application.Modules.Reports.Models.Requests;
using ConferenceBooking.Core.Application.Modules.Reports.Services;
using ConferenceBooking.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ConferenceBooking.Core.Application.Modules.Reports.Handlers;

internal sealed class GetRevenueReportHandler(ConferenceBookingDbContext dbContext)
    : IRequestHandler<GetRevenueReportRequest, RevenueReportModel>
{
    public async Task<RevenueReportModel> Handle(GetRevenueReportRequest request, CancellationToken cancellationToken)
    {
        var period = new ReportPeriod(request.From, request.To);
        // Npgsql translates the time-zone conversion and grouping to SQL. Only daily totals are loaded.
        var dailyTotals = await period.ApplyTo(dbContext.Bookings.AsNoTracking(), request.RoomId)
            .GroupBy(booking => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                booking.StartsAt.UtcDateTime, ReportPeriod.TimeZoneId)))
            .Select(group => new DailyRevenueModel(group.Key, group.Count(),
                group.Sum(booking => booking.RentalCost), group.Sum(booking => booking.ServicesCost),
                group.Sum(booking => booking.TotalCost)))
            .ToDictionaryAsync(day => day.Date, cancellationToken);

        var days = new List<DailyRevenueModel>(request.To.DayNumber - request.From.DayNumber + 1);
        for (var date = request.From; date <= request.To; date = date.AddDays(1))
        {
            days.Add(dailyTotals.GetValueOrDefault(date) ?? new DailyRevenueModel(date, 0, 0, 0, 0));
        }

        // Derive the summary from the same database result so concurrent bookings cannot make it disagree with Days.
        var count = days.Sum(day => day.BookingCount);
        var total = days.Sum(day => day.TotalRevenue);
        var average = count == 0 ? 0 : decimal.Round(total / count, 2, MidpointRounding.AwayFromZero);
        return new RevenueReportModel(request.From, request.To, count, days.Sum(day => day.RentalRevenue),
            days.Sum(day => day.ServicesRevenue), total, average, days);
    }
}
