using ConferenceBooking.Core.Application.Modules.Rooms.Models;
using ConferenceBooking.Core.Application.Modules.Rooms.Models.Requests;
using ConferenceBooking.Data.Entities;
using Riok.Mapperly.Abstractions;

namespace ConferenceBooking.Core.Application.Modules.Rooms.Adapters;

[Mapper]
internal static partial class RoomsAdapter
{
    [MapValue(nameof(RoomEntity.Id), Use = nameof(NewId))]
    [MapValue(nameof(RoomEntity.Version), Use = nameof(NewId))]
    [MapperIgnoreTarget(nameof(RoomEntity.IsDeleted))]
    [MapProperty(nameof(AddRoomRequest.Name), nameof(RoomEntity.Name), Use = nameof(TrimName))]
    internal static partial RoomEntity ToEntity(this AddRoomRequest request);

    [MapperIgnoreSource(nameof(RoomServiceInput.Id))]
    [MapValue(nameof(RoomServiceEntity.Id), Use = nameof(NewId))]
    [MapperIgnoreTarget(nameof(RoomServiceEntity.RoomId))]
    [MapperIgnoreTarget(nameof(RoomServiceEntity.Room))]
    [MapProperty(nameof(RoomServiceInput.Name), nameof(RoomServiceEntity.Name), Use = nameof(TrimName))]
    private static partial RoomServiceEntity ToEntity(RoomServiceInput service);

    [MapperIgnoreSource(nameof(RoomEntity.IsDeleted))]
    [MapperIgnoreSource(nameof(RoomEntity.Version))]
    [MapProperty(nameof(RoomEntity.Services), nameof(RoomModel.Services), Use = nameof(MapServices))]
    internal static partial RoomModel ToModel(this RoomEntity room);

    [MapperIgnoreSource(nameof(RoomServiceEntity.RoomId))]
    [MapperIgnoreSource(nameof(RoomServiceEntity.Room))]
    private static partial RoomServiceModel ToModel(RoomServiceEntity service);

    private static IReadOnlyList<RoomServiceModel> MapServices(ICollection<RoomServiceEntity> services) =>
        [.. services.OrderBy(service => service.Name).ThenBy(service => service.Id).Select(ToModel)];

    private static Guid NewId() => Guid.NewGuid();

    private static string TrimName(string name) => name.Trim();
}
