namespace ConferenceBooking.Core.Application.Modules.Rooms.Models;

/// <summary>
/// Describes a service offered by a room.
/// </summary>
public sealed class RoomServiceInput
{
    /// <summary>The existing service identifier when editing; omitted for a new service.</summary>
    public Guid? Id { get; init; }

    /// <summary>The service name, unique within its room.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The one-time price in UAH, with at most two decimal places.</summary>
    public decimal Price { get; init; }
}
