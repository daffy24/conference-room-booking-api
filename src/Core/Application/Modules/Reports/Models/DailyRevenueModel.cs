namespace ConferenceBooking.Core.Application.Modules.Reports.Models;

/// <summary>Confirmed booking value for one calendar day in Kyiv.</summary>
/// <param name="Date">The local date on which the bookings start.</param>
/// <param name="BookingCount">The number of confirmed bookings.</param>
/// <param name="RentalRevenue">The stored room rental charges.</param>
/// <param name="ServicesRevenue">The stored one-time service charges.</param>
/// <param name="TotalRevenue">The total confirmed booking value.</param>
public sealed record DailyRevenueModel(DateOnly Date, int BookingCount, decimal RentalRevenue,
    decimal ServicesRevenue, decimal TotalRevenue);
