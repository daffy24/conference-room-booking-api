namespace ConferenceBooking.Core.Application.Modules.Bookings.Models;

/// <summary>A service and its accepted price at booking time.</summary>
/// <param name="ServiceId">The original room service identifier.</param>
/// <param name="Name">The service name at booking time.</param>
/// <param name="Price">The one-time charge in UAH.</param>
public sealed record BookingServiceModel(Guid ServiceId, string Name, decimal Price);
