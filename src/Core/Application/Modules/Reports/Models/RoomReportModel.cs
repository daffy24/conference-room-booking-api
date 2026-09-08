namespace ConferenceBooking.Core.Application.Modules.Reports.Models;

/// <summary>Booked time and confirmed booking value for one room.</summary>
/// <param name="RoomId">The stable room identifier.</param>
/// <param name="RoomName">The current room name.</param>
/// <param name="IsDeleted">Whether the room has been removed from the current catalogue.</param>
/// <param name="BookingCount">The number of bookings starting in the requested period.</param>
/// <param name="BookedHours">The total booked duration in hours, rounded to two decimal places.</param>
/// <param name="RentalRevenue">The stored room rental charges.</param>
/// <param name="ServicesRevenue">The stored service charges.</param>
/// <param name="TotalRevenue">The total confirmed booking value.</param>
/// <param name="AverageBookingValue">The average confirmed value, rounded to two decimal places.</param>
public sealed record RoomReportModel(Guid RoomId, string RoomName, bool IsDeleted, int BookingCount,
    decimal BookedHours, decimal RentalRevenue, decimal ServicesRevenue, decimal TotalRevenue,
    decimal AverageBookingValue)
{
    /// <summary>The currency used for all amounts.</summary>
    public string Currency => "UAH";
}
