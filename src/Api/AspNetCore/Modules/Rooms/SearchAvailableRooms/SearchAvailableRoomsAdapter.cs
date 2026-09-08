using ConferenceBooking.Core.Application.Modules.Rooms.Models.Requests;
using Riok.Mapperly.Abstractions;

namespace ConferenceBooking.Api.AspNetCore.Modules.Rooms.SearchAvailableRooms;

[Mapper]
internal static partial class SearchAvailableRoomsAdapter
{
    public static partial SearchAvailableRoomsRequest ToRequest(this SearchAvailableRoomsModel model);
}
