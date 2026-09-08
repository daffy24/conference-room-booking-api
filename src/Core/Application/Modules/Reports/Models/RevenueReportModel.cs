namespace ConferenceBooking.Core.Application.Modules.Reports.Models;

/// <summary>Summarizes confirmed booking value; payment collection is outside this API.</summary>
/// <param name="From">The inclusive first calendar date in Kyiv.</param>
/// <param name="To">The inclusive last calendar date in Kyiv.</param>
/// <param name="BookingCount">The number of confirmed bookings starting in the period.</param>
/// <param name="RentalRevenue">The sum of stored room rental charges.</param>
/// <param name="ServicesRevenue">The sum of stored one-time service charges.</param>
/// <param name="TotalRevenue">The total confirmed booking value.</param>
/// <param name="AverageBookingValue">The average confirmed value, rounded to two decimal places.</param>
/// <param name="Days">One entry for every requested date, including dates with no bookings.</param>
public sealed record RevenueReportModel(DateOnly From, DateOnly To, int BookingCount,
    decimal RentalRevenue, decimal ServicesRevenue, decimal TotalRevenue, decimal AverageBookingValue,
    IReadOnlyList<DailyRevenueModel> Days)
{
    /// <summary>The currency used for all amounts.</summary>
    public string Currency => "UAH";

    /// <summary>The time zone used to interpret report dates.</summary>
    public string TimeZone => "Europe/Kyiv";
}
