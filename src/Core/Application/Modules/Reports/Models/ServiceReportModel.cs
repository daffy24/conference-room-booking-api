namespace ConferenceBooking.Core.Application.Modules.Reports.Models;

/// <summary>Usage and confirmed charges for a service, including removed services.</summary>
/// <param name="RoomId">The room that offered the service.</param>
/// <param name="RoomName">The current room name.</param>
/// <param name="ServiceId">The stable original room service identifier.</param>
/// <param name="ServiceName">The name in the most recently created matching booking snapshot.</param>
/// <param name="BookingCount">The number of bookings that selected this service.</param>
/// <param name="TotalRevenue">The sum of stored one-time charges for this service.</param>
public sealed record ServiceReportModel(Guid RoomId, string RoomName, Guid ServiceId,
    string ServiceName, int BookingCount, decimal TotalRevenue)
{
    /// <summary>The currency used for all amounts.</summary>
    public string Currency => "UAH";
}
