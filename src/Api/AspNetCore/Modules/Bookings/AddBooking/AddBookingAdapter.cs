using ConferenceBooking.Core.Application.Modules.Bookings.Models.Requests;
using Riok.Mapperly.Abstractions;

namespace ConferenceBooking.Api.AspNetCore.Modules.Bookings.AddBooking;

[Mapper]
internal static partial class AddBookingAdapter
{
    public static partial AddBookingRequest ToRequest(this AddBookingModel model, string userId);
}
