using MediatR;

namespace ConferenceBooking.Core.Application.Modules.Bookings.Models.Requests;

/// <summary>Retrieves a booking visible to its owner or an administrator.</summary>
/// <param name="Id">The booking identifier.</param>
/// <param name="UserId">The subject from a validated access token.</param>
/// <param name="IsAdmin">Whether the validated token grants the Admin role.</param>
public sealed record GetBookingRequest(Guid Id, string UserId, bool IsAdmin) : IRequest<BookingModel>;
