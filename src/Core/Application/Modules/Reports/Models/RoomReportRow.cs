namespace ConferenceBooking.Core.Application.Modules.Reports.Models;

internal sealed class RoomReportRow
{
    public Guid RoomId { get; init; }
    public string RoomName { get; init; } = string.Empty;
    public bool IsDeleted { get; init; }
    public int BookingCount { get; init; }
    public decimal BookedMinutes { get; init; }
    public decimal RentalRevenue { get; init; }
    public decimal ServicesRevenue { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal AverageBookingValue => BookingCount == 0
        ? 0 : decimal.Round(TotalRevenue / BookingCount, 2, MidpointRounding.AwayFromZero);
}
