namespace ConferenceBooking.Api.AspNetCore.Modules.Bookings.AddBooking;

/// <summary>Booking input; identity and prices are determined by the server.</summary>
public sealed class AddBookingModel
{
    /// <summary>Room to reserve.</summary>
    public Guid RoomId { get; init; }
    /// <summary>Future start instant in ISO 8601 format with a UTC offset.</summary>
    public DateTimeOffset StartsAt { get; init; }
    /// <summary>Duration in whole minutes, within 06:00–23:00 on one Kyiv calendar day.</summary>
    public int DurationMinutes { get; init; }
    /// <summary>Selected identifiers from the room's available services.</summary>
    public IReadOnlyList<Guid> ServiceIds { get; init; } = [];
}
