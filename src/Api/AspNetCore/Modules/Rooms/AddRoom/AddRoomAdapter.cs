using ConferenceBooking.Core.Application.Modules.Rooms.Models.Requests;
using Riok.Mapperly.Abstractions;

namespace ConferenceBooking.Api.AspNetCore.Modules.Rooms.AddRoom;

[Mapper]
internal static partial class AddRoomAdapter
{
    public static partial AddRoomRequest ToRequest(this AddRoomModel model);
}
