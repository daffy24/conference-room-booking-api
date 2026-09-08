namespace ConferenceBooking.Core.Application.Modules.Rooms.Models;

/// <summary>
/// An available room service.
/// </summary>
/// <param name="Id">The identifier used when booking this service.</param>
/// <param name="Name">The display name.</param>
/// <param name="Price">The one-time price in UAH.</param>
public sealed record RoomServiceModel(Guid Id, string Name, decimal Price);
