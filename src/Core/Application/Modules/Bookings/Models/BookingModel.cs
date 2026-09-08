namespace ConferenceBooking.Core.Application.Modules.Bookings.Models;

/// <summary>
/// A confirmed booking with the prices accepted at creation time.
/// </summary>
/// <param name="Id">The booking identifier.</param>
/// <param name="RoomId">The room identifier.</param>
/// <param name="RoomName">The room name at booking time.</param>
/// <param name="StartsAt">The inclusive start instant in UTC.</param>
/// <param name="EndsAt">The exclusive end instant in UTC.</param>
/// <param name="BaseHourlyRate">The standard hourly rate at booking time.</param>
/// <param name="RentalCost">The room charge after time-of-day adjustments.</param>
/// <param name="ServicesCost">The sum of one-time service charges.</param>
/// <param name="TotalCost">The total confirmed price.</param>
/// <param name="Services">The booked service snapshots.</param>
public sealed record BookingModel(Guid Id, Guid RoomId, string RoomName, DateTimeOffset StartsAt,
    DateTimeOffset EndsAt, decimal BaseHourlyRate, decimal RentalCost, decimal ServicesCost, decimal TotalCost,
    IReadOnlyList<BookingServiceModel> Services)
{
    /// <summary>The currency used for all prices.</summary>
    public string Currency => "UAH";
}
