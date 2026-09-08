using ConferenceBooking.Api.AspNetCore.Modules.Rooms.Models;

namespace ConferenceBooking.Api.AspNetCore.Modules.Rooms.AddRoom;

/// <summary>Details required to create a room.</summary>
public sealed class AddRoomModel
{
    /// <summary>Room display name.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Maximum number of attendees.</summary>
    public int Capacity { get; init; }
    /// <summary>Standard hourly rate in UAH.</summary>
    public decimal BaseHourlyRate { get; init; }
    /// <summary>Available services; an empty list creates a room without services.</summary>
    public IReadOnlyList<RoomServiceModel> Services { get; init; } = [];
}
