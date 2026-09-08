using ConferenceBooking.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConferenceBooking.Data.Postgresql.SeedData;

internal static class ConferenceRoomSeedData
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        // Stable identifiers keep migrations deterministic and make the sample rooms easy to locate.
        var roomA = new Guid("10000000-0000-0000-0000-000000000001");
        var roomB = new Guid("10000000-0000-0000-0000-000000000002");
        var roomC = new Guid("10000000-0000-0000-0000-000000000003");

        modelBuilder.Entity<RoomEntity>().HasData(
            CreateRoom(roomA, "Зал А", 50, 2000m),
            CreateRoom(roomB, "Зал B", 100, 3500m),
            CreateRoom(roomC, "Зал C", 30, 1500m));

        modelBuilder.Entity<RoomServiceEntity>().HasData(
            CreateService("20000000-0000-0000-0000-000000000011", roomA, "Проєктор", 500m),
            CreateService("20000000-0000-0000-0000-000000000012", roomA, "Wi-Fi", 300m),
            CreateService("20000000-0000-0000-0000-000000000013", roomA, "Звук", 700m),
            CreateService("20000000-0000-0000-0000-000000000021", roomB, "Проєктор", 500m),
            CreateService("20000000-0000-0000-0000-000000000022", roomB, "Wi-Fi", 300m),
            CreateService("20000000-0000-0000-0000-000000000023", roomB, "Звук", 700m),
            CreateService("20000000-0000-0000-0000-000000000031", roomC, "Проєктор", 500m),
            CreateService("20000000-0000-0000-0000-000000000032", roomC, "Wi-Fi", 300m),
            CreateService("20000000-0000-0000-0000-000000000033", roomC, "Звук", 700m));
    }

    private static RoomEntity CreateRoom(Guid id, string name, int capacity, decimal baseHourlyRate) => new()
    {
        Id = id,
        Name = name,
        Capacity = capacity,
        BaseHourlyRate = baseHourlyRate,
        Version = id,
    };

    private static RoomServiceEntity CreateService(string id, Guid roomId, string name, decimal price) => new()
    {
        Id = new Guid(id),
        RoomId = roomId,
        Name = name,
        Price = price,
    };
}
