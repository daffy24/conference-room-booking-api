namespace ConferenceBooking.Api.AspNetCore.Modules.Rooms.Models;

/// <summary>A service offered by a room, charged once per booking.</summary>
public sealed class RoomServiceModel
{
    /// <summary>Existing service identifier for an update; omitted for a new service.</summary>
    public Guid? Id { get; init; }
    /// <summary>Service name, unique within the room.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Price in UAH with at most two decimal places.</summary>
    public decimal Price { get; init; }
}
