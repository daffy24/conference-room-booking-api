namespace ConferenceBooking.Core.Application.Modules.Bookings.Services;

/// <summary>
/// Calculates room rental in UAH. Additional services are charged separately by the booking use case.
/// </summary>
public sealed class RentalPriceCalculator
{
    // Non-overlapping periods make the peak rate override the standard daytime rate.
    private static readonly (TimeOnly Start, TimeOnly End, decimal Multiplier)[] Tariffs =
    [
        (BookingSchedule.OpeningTime, new TimeOnly(9, 0), 0.90m),
        (new TimeOnly(9, 0), new TimeOnly(12, 0), 1.00m),
        (new TimeOnly(12, 0), new TimeOnly(14, 0), 1.15m),
        (new TimeOnly(14, 0), new TimeOnly(18, 0), 1.00m),
        (new TimeOnly(18, 0), BookingSchedule.ClosingTime, 0.80m),
    ];

    /// <summary>
    /// Prorates each applicable tariff by minutes and rounds the final rental subtotal to two decimal places.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The base hourly rate is not positive.</exception>
    /// <exception cref="ArgumentException">The period is outside the supported booking schedule.</exception>
    public decimal Calculate(decimal baseHourlyRate, DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baseHourlyRate);

        if (!BookingSchedule.IsValidPeriod(startsAt, endsAt))
            throw new ArgumentException("Bookings must use whole minutes within 06:00–23:00 on one Kyiv calendar day.", nameof(endsAt));

        var localStart = TimeOnly.FromDateTime(BookingSchedule.ToLocalTime(startsAt));
        var localEnd = TimeOnly.FromDateTime(BookingSchedule.ToLocalTime(endsAt));
        var subtotal = 0m;

        foreach (var tariff in Tariffs)
        {
            var overlapStart = localStart > tariff.Start ? localStart : tariff.Start;
            var overlapEnd = localEnd < tariff.End ? localEnd : tariff.End;

            if (overlapStart >= overlapEnd)
                continue;

            var minutes = (decimal)(overlapEnd - overlapStart).Ticks / TimeSpan.TicksPerMinute;
            subtotal += baseHourlyRate * tariff.Multiplier * minutes / 60m;
        }

        // Rounding individual tariff fragments could change the total for a booking crossing a boundary.
        return decimal.Round(subtotal, 2, MidpointRounding.AwayFromZero);
    }
}
