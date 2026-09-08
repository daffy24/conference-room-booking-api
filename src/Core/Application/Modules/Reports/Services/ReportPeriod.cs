using ConferenceBooking.Data.Entities;

namespace ConferenceBooking.Core.Application.Modules.Reports.Services;

internal sealed class ReportPeriod(DateOnly from, DateOnly to)
{
    internal const string TimeZoneId = "Europe/Kyiv";
    private static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);

    // Convert each boundary independently: a Kyiv calendar day can span 23 or 25 UTC hours.
    private DateTimeOffset StartsAt { get; } = ToUtc(from);
    private DateTimeOffset EndsAt { get; } = ToUtc(to.AddDays(1));

    internal IQueryable<BookingEntity> ApplyTo(IQueryable<BookingEntity> bookings, Guid? roomId)
    {
        // Reporting follows the reserved date, not the date the customer placed the booking.
        var query = bookings.Where(booking => booking.StartsAt >= StartsAt && booking.StartsAt < EndsAt);
        return roomId.HasValue ? query.Where(booking => booking.RoomId == roomId.Value) : query;
    }

    private static DateTimeOffset ToUtc(DateOnly date) =>
        new(TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue), TimeZone));
}
