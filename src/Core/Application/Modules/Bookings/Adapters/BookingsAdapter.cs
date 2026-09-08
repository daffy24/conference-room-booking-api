using ConferenceBooking.Core.Application.Modules.Bookings.Models;
using ConferenceBooking.Data.Entities;
using Riok.Mapperly.Abstractions;

namespace ConferenceBooking.Core.Application.Modules.Bookings.Adapters;

[Mapper]
internal static partial class BookingsAdapter
{
    [MapValue(nameof(BookingServiceEntity.Id), Use = nameof(NewId))]
    [MapProperty(nameof(RoomServiceEntity.Id), nameof(BookingServiceEntity.ServiceId))]
    [MapperIgnoreSource(nameof(RoomServiceEntity.RoomId))]
    [MapperIgnoreSource(nameof(RoomServiceEntity.Room))]
    [MapperIgnoreTarget(nameof(BookingServiceEntity.BookingId))]
    [MapperIgnoreTarget(nameof(BookingServiceEntity.Booking))]
    internal static partial BookingServiceEntity ToBookingService(this RoomServiceEntity service);

    [MapperIgnoreSource(nameof(BookingEntity.UserId))]
    [MapperIgnoreSource(nameof(BookingEntity.CreatedAt))]
    [MapperIgnoreSource(nameof(BookingEntity.Room))]
    [MapperIgnoreTarget(nameof(BookingModel.Currency))]
    [MapProperty(nameof(BookingEntity.Services), nameof(BookingModel.Services), Use = nameof(MapServices))]
    internal static partial BookingModel ToModel(this BookingEntity booking);

    [MapperIgnoreSource(nameof(BookingServiceEntity.Id))]
    [MapperIgnoreSource(nameof(BookingServiceEntity.BookingId))]
    [MapperIgnoreSource(nameof(BookingServiceEntity.Booking))]
    private static partial BookingServiceModel ToModel(BookingServiceEntity service);

    private static IReadOnlyList<BookingServiceModel> MapServices(ICollection<BookingServiceEntity> services) =>
        [.. services.OrderBy(service => service.Name).ThenBy(service => service.ServiceId).Select(ToModel)];

    private static Guid NewId() => Guid.NewGuid();
}
