using System.Net;
using System.Text.Json;
using ConferenceBooking.Data;
using ConferenceBooking.Data.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace ConferenceBooking.Api.AspNetCore.Integration.Tests.Modules.Reports;

public abstract class ReportEndpointTestsBase(BusinessAppFactory factory) : BusinessEndpointTests(factory)
{
    protected async Task<RoomEntity> SeedRoomAsync()
    {
        var room = new RoomEntity
        {
            Id = Guid.NewGuid(),
            Name = $"Report room {Guid.NewGuid():N}",
            Capacity = 50,
            BaseHourlyRate = 2000m,
            Version = Guid.NewGuid(),
        };
        room.Services =
        [
            new RoomServiceEntity { Id = Guid.NewGuid(), RoomId = room.Id, Name = "Projector", Price = 500m },
            new RoomServiceEntity { Id = Guid.NewGuid(), RoomId = room.Id, Name = "Wi-Fi", Price = 300m },
        ];

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConferenceBookingDbContext>();
        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync(CancellationToken);
        return room;
    }

    protected static BookingEntity CreateBooking(
        RoomEntity room,
        DateTimeOffset startsAt,
        int durationMinutes,
        decimal rentalCost,
        params RoomServiceEntity[] services)
    {
        var booking = new BookingEntity
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            RoomName = room.Name,
            UserId = "report-customer",
            BaseHourlyRate = room.BaseHourlyRate,
            StartsAt = startsAt.ToUniversalTime(),
            EndsAt = startsAt.AddMinutes(durationMinutes).ToUniversalTime(),
            CreatedAt = startsAt.AddDays(-7).ToUniversalTime(),
            RentalCost = rentalCost,
            ServicesCost = services.Sum(service => service.Price),
            TotalCost = rentalCost + services.Sum(service => service.Price),
        };
        booking.Services = services.Select(service => new BookingServiceEntity
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            ServiceId = service.Id,
            Name = service.Name,
            Price = service.Price,
        }).ToArray();
        return booking;
    }

    protected async Task SeedBookingsAsync(params BookingEntity[] bookings)
    {
        // Historical data cannot be created through the API, which correctly rejects past booking starts.
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConferenceBookingDbContext>();
        dbContext.Bookings.AddRange(bookings);
        await dbContext.SaveChangesAsync(CancellationToken);
    }

    protected static async Task<JsonElement> ReadReportAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadJsonAsync(response);
    }

    protected static string ReportPath(string report, string from, string to, Guid? roomId = null, int page = 1, int pageSize = 20)
    {
        var path = $"/api/reports/{report}?from={from}&to={to}";
        if (roomId.HasValue)
            path += $"&roomId={roomId.Value}";

        return report == "revenue" ? path : $"{path}&page={page}&pageSize={pageSize}";
    }

    protected static void AssertRevenue(JsonElement report, int bookingCount, decimal rentalRevenue, decimal servicesRevenue)
    {
        Assert.Equal(bookingCount, report.GetProperty("bookingCount").GetInt32());
        Assert.Equal(rentalRevenue, report.GetProperty("rentalRevenue").GetDecimal());
        Assert.Equal(servicesRevenue, report.GetProperty("servicesRevenue").GetDecimal());
        Assert.Equal(rentalRevenue + servicesRevenue, report.GetProperty("totalRevenue").GetDecimal());
    }

    protected static DateTimeOffset WinterStart(int day, int hour = 10) =>
        new(2024, 1, day, hour, 0, 0, TimeSpan.FromHours(2));
}
