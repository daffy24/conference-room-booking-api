namespace ConferenceBooking.Core.Application.Modules.Rooms.Models;

/// <summary>
/// A conference room and the services currently offered with it. Prices are in UAH.
/// </summary>
/// <param name="Id">The room identifier.</param>
/// <param name="Name">The display name.</param>
/// <param name="Capacity">The maximum number of attendees.</param>
/// <param name="BaseHourlyRate">The standard hourly rental price.</param>
/// <param name="Services">Available services, charged once per booking.</param>
public sealed record RoomModel(Guid Id, string Name, int Capacity, decimal BaseHourlyRate,
    IReadOnlyList<RoomServiceModel> Services);
