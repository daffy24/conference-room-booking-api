namespace ConferenceBooking.Core.Application.Modules.Bookings.Services;

/// <summary>
/// Defines booking hours in the venue's time zone, independently of the caller's UTC offset.
/// </summary>
public static class BookingSchedule
{
    private static readonly TimeZoneInfo VenueTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Kyiv");

    /// <summary>
    /// Gets the first time at which a booking may start.
    /// </summary>
    public static TimeOnly OpeningTime { get; } = new(6, 0);

    /// <summary>
    /// Gets the latest time at which a booking may end.
    /// </summary>
    public static TimeOnly ClosingTime { get; } = new(23, 0);

    /// <summary>
    /// Converts an instant to the venue's local calendar date and time, including daylight saving rules.
    /// </summary>
    public static DateTime ToLocalTime(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, VenueTimeZone).DateTime;

    /// <summary>
    /// Checks that a positive, minute-aligned interval falls within one local business day.
    /// </summary>
    public static bool IsValidPeriod(DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        if (endsAt <= startsAt || !IsMinuteAligned(startsAt) || !IsMinuteAligned(endsAt))
            return false;

        var localStart = ToLocalTime(startsAt);
        var localEnd = ToLocalTime(endsAt);

        return localStart.Date == localEnd.Date
            && TimeOnly.FromDateTime(localStart) >= OpeningTime
            && TimeOnly.FromDateTime(localEnd) <= ClosingTime;
    }

    private static bool IsMinuteAligned(DateTimeOffset instant) => instant.Ticks % TimeSpan.TicksPerMinute == 0;
}
