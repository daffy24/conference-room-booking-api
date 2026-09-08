namespace ConferenceBooking.Core.Application.Modules.Reports.Models;

internal sealed class ServiceReportRow
{
    public Guid RoomId { get; init; }
    public string RoomName { get; init; } = string.Empty;
    public Guid ServiceId { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public int BookingCount { get; init; }
    public decimal TotalRevenue { get; init; }
}
