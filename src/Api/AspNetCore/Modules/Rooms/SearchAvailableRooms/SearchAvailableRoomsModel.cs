namespace ConferenceBooking.Api.AspNetCore.Modules.Rooms.SearchAvailableRooms;

/// <summary>Availability criteria. Intervals include their start and exclude their end.</summary>
public sealed class SearchAvailableRoomsModel
{
    /// <summary>Start instant in ISO 8601 format with a UTC offset.</summary>
    public DateTimeOffset StartsAt { get; init; }
    /// <summary>End instant in ISO 8601 format with a UTC offset.</summary>
    public DateTimeOffset EndsAt { get; init; }
    /// <summary>Minimum required capacity.</summary>
    public int Capacity { get; init; }
    /// <summary>One-based page number.</summary>
    public int Page { get; init; } = 1;
    /// <summary>Number of rooms per page, from 1 to 100.</summary>
    public int PageSize { get; init; } = 20;
}
