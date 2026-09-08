using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConferenceBooking.Data;
using ConferenceBooking.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ConferenceBooking.Api.AspNetCore.Integration.Tests.Modules.Bookings;

[Trait("Category", "Postgresql")]
public sealed class BookingEndpointsTests(BusinessAppFactory factory) : BusinessEndpointTests(factory), IClassFixture<BusinessAppFactory>
{
    [BusinessFact]
    public async Task BookingChargesEachTariffPeriodAndServiceOnce()
    {
        using var admin = CreateClient("Admin");
        var room = await CreateRoomAsync(admin);
        var roomId = room.GetProperty("id").GetGuid();
        var serviceId = room.GetProperty("services")[0].GetProperty("id").GetGuid();
        using var customer = CreateClient();

        using var response = await customer.PostAsJsonAsync("/api/bookings", new
        {
            roomId,
            startsAt = FutureStart(11),
            durationMinutes = 240,
            serviceIds = new[] { serviceId },
        }, CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var booking = await ReadJsonAsync(response);
        Assert.NotEqual(Guid.Empty, booking.GetProperty("id").GetGuid());
        Assert.Equal(roomId, booking.GetProperty("roomId").GetGuid());
        Assert.Equal(FutureStart(11), booking.GetProperty("startsAt").GetDateTimeOffset());
        Assert.Equal(FutureStart(15), booking.GetProperty("endsAt").GetDateTimeOffset());
        Assert.Equal(2000m, booking.GetProperty("baseHourlyRate").GetDecimal());
        // 11:00–12:00 and 14:00–15:00 cost 2,000/hour; 12:00–14:00 costs 2,300/hour.
        Assert.Equal(8600m, booking.GetProperty("rentalCost").GetDecimal());
        Assert.Equal(500m, booking.GetProperty("servicesCost").GetDecimal());
        Assert.Equal(9100m, booking.GetProperty("totalCost").GetDecimal());
        Assert.Equal("UAH", booking.GetProperty("currency").GetString());
        Assert.Single(booking.GetProperty("services").EnumerateArray());
        Assert.Equal(serviceId, booking.GetProperty("services")[0].GetProperty("serviceId").GetGuid());
    }

    [BusinessFact]
    public async Task PartialHoursAreProratedAcrossTariffBoundaries()
    {
        using var admin = CreateClient("Admin");
        var room = await CreateRoomAsync(admin);
        using var customer = CreateClient();

        using var response = await customer.PostAsJsonAsync("/api/bookings", new
        {
            roomId = room.GetProperty("id").GetGuid(),
            startsAt = FutureStart(8, 30),
            durationMinutes = 360,
            serviceIds = Array.Empty<Guid>(),
        }, CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var booking = await ReadJsonAsync(response);
        // 0.5 morning hour + 3 standard hours + 2 peak hours + 0.5 standard hour.
        Assert.Equal(12500m, booking.GetProperty("rentalCost").GetDecimal());
        Assert.Equal(0m, booking.GetProperty("servicesCost").GetDecimal());
        Assert.Equal(12500m, booking.GetProperty("totalCost").GetDecimal());
    }

    [BusinessFact]
    public async Task BookingPreservesPricesAndNamesAfterRoomChanges()
    {
        using var admin = CreateClient("Admin");
        var room = await CreateRoomAsync(admin);
        var roomId = room.GetProperty("id").GetGuid();
        var serviceId = room.GetProperty("services")[0].GetProperty("id").GetGuid();
        using var customer = CreateClient();
        var booking = await CreateBookingAsync(customer, room, FutureStart(10), serviceIds: [serviceId]);

        using var updateResponse = await admin.PatchAsJsonAsync($"/api/rooms/{roomId}", new
        {
            name = "Renamed room",
            baseHourlyRate = 7000m,
            services = new[] { new { id = serviceId, name = "Updated projector", price = 900m } },
        }, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var response = await customer.GetAsync($"/api/bookings/{booking.GetProperty("id").GetGuid()}", CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var snapshot = await ReadJsonAsync(response);
        Assert.Equal(room.GetProperty("name").GetString(), snapshot.GetProperty("roomName").GetString());
        Assert.Equal(2000m, snapshot.GetProperty("baseHourlyRate").GetDecimal());
        Assert.Equal(2500m, snapshot.GetProperty("totalCost").GetDecimal());
        Assert.Equal("Projector", snapshot.GetProperty("services")[0].GetProperty("name").GetString());
        Assert.Equal(500m, snapshot.GetProperty("services")[0].GetProperty("price").GetDecimal());
    }

    [BusinessFact]
    public async Task OnlyBookingOwnerAndAdministratorCanReadBooking()
    {
        using var admin = CreateClient("Admin");
        var room = await CreateRoomAsync(admin);
        using var owner = CreateClient(subject: "booking-owner");
        var booking = await CreateBookingAsync(owner, room, FutureStart(10));
        var path = $"/api/bookings/{booking.GetProperty("id").GetGuid()}";
        using var stranger = CreateClient(subject: "another-customer");

        using var ownerResponse = await owner.GetAsync(path, CancellationToken);
        using var adminResponse = await admin.GetAsync(path, CancellationToken);
        using var strangerResponse = await stranger.GetAsync(path, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
        await AssertProblemAsync(strangerResponse, HttpStatusCode.NotFound);
    }

    [BusinessFact]
    public async Task OverlapIsRejectedButAdjacentBookingsAreAllowed()
    {
        using var admin = CreateClient("Admin");
        var room = await CreateRoomAsync(admin);
        var roomId = room.GetProperty("id").GetGuid();
        using var customer = CreateClient();
        await CreateBookingAsync(customer, room, FutureStart(10), durationMinutes: 120);

        using var overlapResponse = await customer.PostAsJsonAsync("/api/bookings", new
        {
            roomId,
            startsAt = FutureStart(11),
            durationMinutes = 120,
            serviceIds = Array.Empty<Guid>(),
        }, CancellationToken);
        await AssertProblemAsync(overlapResponse, HttpStatusCode.Conflict);

        using var availableResponse = await customer.GetAsync(AvailabilityPath(FutureStart(11), FutureStart(12)), CancellationToken);
        Assert.Equal(HttpStatusCode.OK, availableResponse.StatusCode);
        Assert.DoesNotContain((await ReadJsonAsync(availableResponse)).GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == roomId);

        // Half-open intervals allow one meeting to begin exactly when the previous meeting ends.
        await CreateBookingAsync(customer, room, FutureStart(12));
        await CreateBookingAsync(customer, room, FutureStart(9));
    }

    [BusinessFact]
    public async Task ConcurrentOverlappingRequestsPersistOnlyOneBooking()
    {
        using var admin = CreateClient("Admin");
        var room = await CreateRoomAsync(admin);
        using var firstCustomer = CreateClient(subject: "concurrent-customer-one");
        using var secondCustomer = CreateClient(subject: "concurrent-customer-two");
        var request = new
        {
            roomId = room.GetProperty("id").GetGuid(),
            startsAt = FutureStart(10),
            durationMinutes = 60,
            serviceIds = Array.Empty<Guid>(),
        };

        // Separate HTTP requests create independent DI scopes, database contexts and transactions.
        var responses = await Task.WhenAll(
            firstCustomer.PostAsJsonAsync("/api/bookings", request, CancellationToken),
            secondCustomer.PostAsJsonAsync("/api/bookings", request, CancellationToken));
        using var firstResponse = responses[0];
        using var secondResponse = responses[1];

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        var conflict = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        await AssertProblemAsync(conflict, HttpStatusCode.Conflict);
    }

    [BusinessFact]
    public async Task PostgreSqlRejectsOverlapEvenWhenApplicationChecksAreBypassed()
    {
        using var admin = CreateClient("Admin");
        var room = await CreateRoomAsync(admin);
        using var customer = CreateClient();
        await CreateBookingAsync(customer, room, FutureStart(10));
        var roomId = room.GetProperty("id").GetGuid();
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConferenceBookingDbContext>();

        // Bypass the handler and room version to prove that PostgreSQL itself protects the reservation interval.
        dbContext.Bookings.Add(new BookingEntity
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            UserId = "direct-database-writer",
            RoomName = room.GetProperty("name").GetString()!,
            BaseHourlyRate = 2000m,
            StartsAt = FutureStart(10, 30).ToUniversalTime(),
            EndsAt = FutureStart(11, 30).ToUniversalTime(),
            CreatedAt = DateTimeOffset.UtcNow,
            RentalCost = 2000m,
            ServicesCost = 0m,
            TotalCost = 2000m,
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync(CancellationToken));
        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.ExclusionViolation, postgresException.SqlState);
        Assert.Equal(1, await dbContext.Bookings.AsNoTracking().CountAsync(booking => booking.RoomId == roomId, CancellationToken));
    }

    [BusinessFact]
    public async Task RoomWithFutureBookingCannotBeDeleted()
    {
        using var admin = CreateClient("Admin");
        var room = await CreateRoomAsync(admin);
        using var customer = CreateClient();
        await CreateBookingAsync(customer, room, FutureStart(10));

        using var response = await admin.DeleteAsync($"/api/rooms/{room.GetProperty("id").GetGuid()}", CancellationToken);
        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [BusinessFact]
    public async Task ForeignAndDuplicateServiceIdsAreRejected()
    {
        using var admin = CreateClient("Admin");
        var room = await CreateRoomAsync(admin);
        var anotherRoom = await CreateRoomAsync(admin);
        var ownServiceId = room.GetProperty("services")[0].GetProperty("id").GetGuid();
        var foreignServiceId = anotherRoom.GetProperty("services")[0].GetProperty("id").GetGuid();
        var invalidSelections = new[] { new[] { ownServiceId, ownServiceId }, new[] { foreignServiceId }, new[] { Guid.NewGuid() } };
        using var customer = CreateClient();

        foreach (var serviceIds in invalidSelections)
        {
            using var response = await customer.PostAsJsonAsync("/api/bookings", new
            {
                roomId = room.GetProperty("id").GetGuid(),
                startsAt = FutureStart(10),
                durationMinutes = 60,
                serviceIds,
            }, CancellationToken);
            await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        }
    }

    [BusinessFact]
    public async Task NullSelectedServiceListReturnsValidationProblem()
    {
        using var admin = CreateClient("Admin");
        var room = await CreateRoomAsync(admin);
        using var customer = CreateClient();

        using var response = await customer.PostAsJsonAsync("/api/bookings", new
        {
            roomId = room.GetProperty("id").GetGuid(),
            startsAt = FutureStart(10),
            durationMinutes = 60,
            serviceIds = (Guid[]?)null,
        }, CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [BusinessFact]
    public async Task InvalidBookingTimesAreRejectedBeforeSaving()
    {
        using var admin = CreateClient("Admin");
        var room = await CreateRoomAsync(admin);
        var invalidPeriods = new[]
        {
            (StartsAt: FutureStart(5), DurationMinutes: 60),
            (StartsAt: FutureStart(22, 30), DurationMinutes: 60),
            (StartsAt: FutureStart(10), DurationMinutes: 0),
            (StartsAt: FutureStart(10).AddSeconds(1), DurationMinutes: 60),
            (StartsAt: FutureStart(10).AddYears(-2), DurationMinutes: 60),
            (StartsAt: FutureStart(10), DurationMinutes: 24 * 60),
        };
        using var customer = CreateClient();

        foreach (var period in invalidPeriods)
        {
            using var response = await customer.PostAsJsonAsync("/api/bookings", new
            {
                roomId = room.GetProperty("id").GetGuid(),
                startsAt = period.StartsAt,
                durationMinutes = period.DurationMinutes,
                serviceIds = Array.Empty<Guid>(),
            }, CancellationToken);
            await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        }
    }

    [BusinessFact]
    public async Task BookingTimestampWithoutExplicitOffsetIsRejected()
    {
        using var admin = CreateClient("Admin");
        var room = await CreateRoomAsync(admin);
        using var customer = CreateClient();
        var invalidStarts = new[]
        {
            FutureStart(10).ToString("yyyy-MM-dd'T'HH:mm:ss"),
            FutureStart(10).ToString("yyyy-MM-dd'T'HH:mm"),
            FutureStart(10).ToString("yyyy-MM-dd'T'HH:mm:ss.fff"),
        };

        foreach (var startsAt in invalidStarts)
        {
            using var response = await customer.PostAsJsonAsync("/api/bookings", new
            {
                roomId = room.GetProperty("id").GetGuid(),
                startsAt,
                durationMinutes = 60,
                serviceIds = Array.Empty<Guid>(),
            }, CancellationToken);
            await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        }
    }

    [BusinessFact]
    public async Task ExplicitUtcAndNumericOffsetsProduceTheSameBookingInstantAndPrice()
    {
        using var admin = CreateClient("Admin");
        using var customer = CreateClient();
        var expectedStart = FutureStart(12).ToUniversalTime();
        var equivalentStarts = new[]
        {
            expectedStart.UtcDateTime.ToString("O"),
            expectedStart.ToOffset(TimeSpan.FromHours(2)).ToString("O"),
            expectedStart.ToOffset(TimeSpan.FromHours(3)).ToString("O"),
        };

        foreach (var startsAt in equivalentStarts)
        {
            // Separate rooms prevent the reservation guard from obscuring timestamp parsing.
            var room = await CreateRoomAsync(admin);
            using var response = await customer.PostAsJsonAsync("/api/bookings", new
            {
                roomId = room.GetProperty("id").GetGuid(),
                startsAt,
                durationMinutes = 60,
                serviceIds = Array.Empty<Guid>(),
            }, CancellationToken);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var booking = await ReadJsonAsync(response);
            var actualStart = booking.GetProperty("startsAt").GetDateTimeOffset();
            Assert.Equal(expectedStart, actualStart);
            Assert.Equal(TimeSpan.Zero, actualStart.Offset);
            Assert.Equal(expectedStart.AddHours(1), booking.GetProperty("endsAt").GetDateTimeOffset());
            Assert.Equal(2300m, booking.GetProperty("rentalCost").GetDecimal());
            Assert.Equal(2300m, booking.GetProperty("totalCost").GetDecimal());
        }
    }

    [BusinessFact]
    public async Task DeletedRoomCannotBeBooked()
    {
        using var admin = CreateClient("Admin");
        var room = await CreateRoomAsync(admin);
        var roomId = room.GetProperty("id").GetGuid();
        using var deleteResponse = await admin.DeleteAsync($"/api/rooms/{roomId}", CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        using var customer = CreateClient();

        using var response = await customer.PostAsJsonAsync("/api/bookings", new
        {
            roomId,
            startsAt = FutureStart(10),
            durationMinutes = 60,
            serviceIds = Array.Empty<Guid>(),
        }, CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    private static async Task<JsonElement> CreateBookingAsync(
        HttpClient client, JsonElement room, DateTimeOffset startsAt, int durationMinutes = 60, Guid[]? serviceIds = null)
    {
        using var response = await client.PostAsJsonAsync("/api/bookings", new
        {
            roomId = room.GetProperty("id").GetGuid(),
            startsAt,
            durationMinutes,
            serviceIds = serviceIds ?? [],
        }, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadJsonAsync(response);
    }
}
