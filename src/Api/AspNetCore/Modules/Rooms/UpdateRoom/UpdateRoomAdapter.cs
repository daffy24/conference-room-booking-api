using ConferenceBooking.Core.Application.Modules.Rooms.Models.Requests;
using Riok.Mapperly.Abstractions;

namespace ConferenceBooking.Api.AspNetCore.Modules.Rooms.UpdateRoom;

[Mapper]
internal static partial class UpdateRoomAdapter
{
    public static partial UpdateRoomRequest ToRequest(this UpdateRoomModel model, Guid id);
}
