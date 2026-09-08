using System.Net;
using System.Net.Http.Json;
using ConferenceBooking.Data.Entities;

namespace ConferenceBooking.Api.AspNetCore.Integration.Tests.Modules.Reports;

[Trait("Category", "Postgresql")]
public sealed class RoomAndServiceReportTests(BusinessAppFactory factory) : ReportEndpointTestsBase(factory), IClassFixture<BusinessAppFactory>
{
    [BusinessFact]
    public async Task RoomReportIncludesActiveRoomsWithoutBookings()
    {
        var room = await SeedRoomAsync();
        using var admin = CreateClient("Admin");

        var report = await ReadReportAsync(admin, ReportPath("rooms", "2024-01-15", "2024-01-17", room.Id));

        Assert.Equal(1, report.GetProperty("totalCount").GetInt32());
        var item = Assert.Single(report.GetProperty("items").EnumerateArray());
        Assert.Equal(room.Id, item.GetProperty("roomId").GetGuid());
        Assert.Equal(room.Name, item.GetProperty("roomName").GetString());
        Assert.False(item.GetProperty("isDeleted").GetBoolean());
        AssertRevenue(item, 0, 0m, 0m);
        Assert.Equal(0m, item.GetProperty("bookedHours").GetDecimal());
        Assert.Equal(0m, item.GetProperty("averageBookingValue").GetDecimal());
        Assert.Equal("UAH", item.GetProperty("currency").GetString());
    }

    [BusinessFact]
    public async Task RoomReportCountsDurationAndRevenueOncePerBooking()
    {
        var room = await SeedRoomAsync();
        var projector = room.Services.Single(service => service.Name == "Projector");
        await SeedBookingsAsync(
            CreateBooking(room, WinterStart(15), 60, 2000m, room.Services.ToArray()),
            CreateBooking(room, WinterStart(16, 14), 90, 3000m, projector));
        using var admin = CreateClient("Admin");

        var report = await ReadReportAsync(admin, ReportPath("rooms", "2024-01-15", "2024-01-17", room.Id, pageSize: 1));

        Assert.Equal(1, report.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, report.GetProperty("page").GetInt32());
        Assert.Equal(1, report.GetProperty("pageSize").GetInt32());
        var item = Assert.Single(report.GetProperty("items").EnumerateArray());
        AssertRevenue(item, 2, 5000m, 1300m);
        Assert.Equal(2.5m, item.GetProperty("bookedHours").GetDecimal());
        Assert.Equal(3150m, item.GetProperty("averageBookingValue").GetDecimal());

        var nextPage = await ReadReportAsync(admin, ReportPath("rooms", "2024-01-15", "2024-01-17", room.Id, page: 2, pageSize: 1));
        Assert.Equal(1, nextPage.GetProperty("totalCount").GetInt32());
        Assert.Empty(nextPage.GetProperty("items").EnumerateArray());
    }

    [BusinessFact]
    public async Task RoomsAreRankedByBookedRevenueBeforePagination()
    {
        var higherRevenueRoom = await SeedRoomAsync();
        var lowerRevenueRoom = await SeedRoomAsync();
        var startsAt = new DateTimeOffset(2023, 2, 10, 10, 0, 0, TimeSpan.FromHours(2));
        await SeedBookingsAsync(
            CreateBooking(higherRevenueRoom, startsAt, 120, 4000m, higherRevenueRoom.Services.ToArray()),
            CreateBooking(lowerRevenueRoom, startsAt, 60, 2000m));
        using var admin = CreateClient("Admin");

        var firstPage = await ReadReportAsync(admin, ReportPath("rooms", "2023-02-10", "2023-02-10", pageSize: 1));
        var secondPage = await ReadReportAsync(admin, ReportPath("rooms", "2023-02-10", "2023-02-10", page: 2, pageSize: 1));

        Assert.True(firstPage.GetProperty("totalCount").GetInt32() >= 2);
        var firstRoom = Assert.Single(firstPage.GetProperty("items").EnumerateArray());
        var secondRoom = Assert.Single(secondPage.GetProperty("items").EnumerateArray());
        Assert.Equal(higherRevenueRoom.Id, firstRoom.GetProperty("roomId").GetGuid());
        Assert.Equal(lowerRevenueRoom.Id, secondRoom.GetProperty("roomId").GetGuid());
        AssertRevenue(firstRoom, 1, 4000m, 800m);
        AssertRevenue(secondRoom, 1, 2000m, 0m);
    }

    [BusinessFact]
    public async Task ServiceReportRanksSelectionCountsAndPagesServiceGroups()
    {
        var room = await SeedRoomAsync();
        var projector = room.Services.Single(service => service.Name == "Projector");
        var wifi = room.Services.Single(service => service.Name == "Wi-Fi");
        await SeedBookingsAsync(
            CreateBooking(room, WinterStart(15), 60, 2000m, room.Services.ToArray()),
            CreateBooking(room, WinterStart(16, 14), 90, 3000m, projector));
        using var admin = CreateClient("Admin");

        var firstPage = await ReadReportAsync(admin, ReportPath("services", "2024-01-15", "2024-01-17", room.Id, pageSize: 1));
        var secondPage = await ReadReportAsync(admin, ReportPath("services", "2024-01-15", "2024-01-17", room.Id, page: 2, pageSize: 1));

        Assert.Equal(2, firstPage.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, secondPage.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, secondPage.GetProperty("page").GetInt32());
        Assert.Equal(1, secondPage.GetProperty("pageSize").GetInt32());
        var first = Assert.Single(firstPage.GetProperty("items").EnumerateArray());
        var second = Assert.Single(secondPage.GetProperty("items").EnumerateArray());
        Assert.Equal(room.Id, first.GetProperty("roomId").GetGuid());
        Assert.Equal(room.Name, first.GetProperty("roomName").GetString());
        Assert.Equal(projector.Id, first.GetProperty("serviceId").GetGuid());
        Assert.Equal("Projector", first.GetProperty("serviceName").GetString());
        Assert.Equal(2, first.GetProperty("bookingCount").GetInt32());
        Assert.Equal(1000m, first.GetProperty("totalRevenue").GetDecimal());
        Assert.Equal("UAH", first.GetProperty("currency").GetString());
        Assert.Equal(wifi.Id, second.GetProperty("serviceId").GetGuid());
        Assert.Equal(1, second.GetProperty("bookingCount").GetInt32());
        Assert.Equal(300m, second.GetProperty("totalRevenue").GetDecimal());
    }

    [BusinessFact]
    public async Task ReportsPreserveHistoryAfterPriceChangesServiceRemovalAndRoomDeletion()
    {
        var room = await SeedRoomAsync();
        var projector = room.Services.Single(service => service.Name == "Projector");
        var laterCreatedBooking = CreateBooking(room, WinterStart(15), 60, 2000m, projector);
        laterCreatedBooking.CreatedAt = WinterStart(10).ToUniversalTime();
        laterCreatedBooking.Services.Single().Name = "Latest agreed projector";
        var earlierCreatedBooking = CreateBooking(room, WinterStart(16), 60, 2000m, new RoomServiceEntity
        {
            Id = projector.Id,
            Name = "Earlier projector name",
            Price = 700m,
        });
        earlierCreatedBooking.CreatedAt = WinterStart(9).ToUniversalTime();
        await SeedBookingsAsync(laterCreatedBooking, earlierCreatedBooking);
        using var admin = CreateClient("Admin");

        using var updateResponse = await admin.PatchAsJsonAsync($"/api/rooms/{room.Id}", new
        {
            name = "Renamed historical room",
            baseHourlyRate = 9000m,
            services = Array.Empty<object>(),
        }, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        using var deleteResponse = await admin.DeleteAsync($"/api/rooms/{room.Id}", CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var revenue = await ReadReportAsync(admin, ReportPath("revenue", "2024-01-15", "2024-01-16", room.Id));
        AssertRevenue(revenue, 2, 4000m, 1200m);
        var rooms = await ReadReportAsync(admin, ReportPath("rooms", "2024-01-15", "2024-01-16", room.Id));
        var roomItem = Assert.Single(rooms.GetProperty("items").EnumerateArray());
        Assert.True(roomItem.GetProperty("isDeleted").GetBoolean());
        Assert.Equal("Renamed historical room", roomItem.GetProperty("roomName").GetString());
        AssertRevenue(roomItem, 2, 4000m, 1200m);
        Assert.Equal(2m, roomItem.GetProperty("bookedHours").GetDecimal());
        Assert.Equal(2600m, roomItem.GetProperty("averageBookingValue").GetDecimal());

        var services = await ReadReportAsync(admin, ReportPath("services", "2024-01-15", "2024-01-16", room.Id));
        Assert.Equal(1, services.GetProperty("totalCount").GetInt32());
        var serviceItem = Assert.Single(services.GetProperty("items").EnumerateArray());
        Assert.Equal(projector.Id, serviceItem.GetProperty("serviceId").GetGuid());
        // A rename keeps one service group; its label comes from creation order, not the meeting date.
        Assert.Equal("Latest agreed projector", serviceItem.GetProperty("serviceName").GetString());
        Assert.Equal("Renamed historical room", serviceItem.GetProperty("roomName").GetString());
        Assert.Equal(2, serviceItem.GetProperty("bookingCount").GetInt32());
        Assert.Equal(1200m, serviceItem.GetProperty("totalRevenue").GetDecimal());

        var emptyPeriod = await ReadReportAsync(admin, ReportPath("rooms", "2024-01-17", "2024-01-17", room.Id));
        Assert.Equal(0, emptyPeriod.GetProperty("totalCount").GetInt32());
        Assert.Empty(emptyPeriod.GetProperty("items").EnumerateArray());
    }

    [BusinessFact]
    public async Task UnselectedServicesAreOmittedWhileFreeSelectedServicesAreCounted()
    {
        var room = await SeedRoomAsync();
        var freeService = room.Services.Single(service => service.Name == "Wi-Fi");
        freeService.Price = 0m;
        await SeedBookingsAsync(CreateBooking(room, WinterStart(15), 60, 2000m, freeService));
        using var admin = CreateClient("Admin");

        var report = await ReadReportAsync(admin, ReportPath("services", "2024-01-15", "2024-01-15", room.Id));

        Assert.Equal(1, report.GetProperty("totalCount").GetInt32());
        var item = Assert.Single(report.GetProperty("items").EnumerateArray());
        Assert.Equal(freeService.Id, item.GetProperty("serviceId").GetGuid());
        Assert.Equal(1, item.GetProperty("bookingCount").GetInt32());
        Assert.Equal(0m, item.GetProperty("totalRevenue").GetDecimal());
    }

    [BusinessFact]
    public async Task ServiceReportFiltersBookingsByMeetingDateAndRoom()
    {
        var room = await SeedRoomAsync();
        var anotherRoom = await SeedRoomAsync();
        var projector = room.Services.Single(service => service.Name == "Projector");
        await SeedBookingsAsync(
            CreateBooking(room, WinterStart(14), 60, 2000m, projector),
            CreateBooking(room, WinterStart(15), 60, 2000m, projector),
            CreateBooking(room, WinterStart(16), 60, 2000m, projector),
            CreateBooking(anotherRoom, WinterStart(15), 60, 2000m, anotherRoom.Services.ToArray()));
        using var admin = CreateClient("Admin");

        var report = await ReadReportAsync(admin, ReportPath("services", "2024-01-15", "2024-01-15", room.Id));

        Assert.Equal(1, report.GetProperty("totalCount").GetInt32());
        var item = Assert.Single(report.GetProperty("items").EnumerateArray());
        Assert.Equal(room.Id, item.GetProperty("roomId").GetGuid());
        Assert.Equal(1, item.GetProperty("bookingCount").GetInt32());
        Assert.Equal(500m, item.GetProperty("totalRevenue").GetDecimal());
    }
}
