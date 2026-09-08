using ConferenceBooking.Api.AspNetCore.Modules.Rooms.Models;

namespace ConferenceBooking.Api.AspNetCore.Modules.Rooms.UpdateRoom;

/// <summary>Partial room update. Omitted or null properties retain their current values.</summary>
public sealed class UpdateRoomModel
{
    /// <summary>Updated room display name.</summary>
    public string? Name { get; init; }
    /// <summary>Updated maximum number of attendees.</summary>
    public int? Capacity { get; init; }
    /// <summary>Updated standard hourly rate in UAH.</summary>
    public decimal? BaseHourlyRate { get; init; }
    /// <summary>Replaces the service list when supplied; an empty list removes all services.</summary>
    public IReadOnlyList<RoomServiceModel>? Services { get; init; }
}
