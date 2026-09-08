using System.Net;
using System.Net.Http.Json;

namespace ConferenceBooking.Api.AspNetCore.Integration.Tests.Modules.Rooms;

[Trait("Category", "Postgresql")]
public sealed class RoomEndpointsTests(BusinessAppFactory factory) : BusinessEndpointTests(factory), IClassFixture<BusinessAppFactory>
{
    [BusinessFact]
    public async Task ExistingServiceNamesCanBeSwappedWithoutChangingTheirIdentifiers()
    {
        using var admin = CreateClient("Admin");
        using var createResponse = await admin.PostAsJsonAsync("/api/rooms", new
        {
            name = "Room with renamed services",
            capacity = 50,
            baseHourlyRate = 2000m,
            services = new[] { new { name = "Projector", price = 500m }, new { name = "Sound", price = 700m } },
        }, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var room = await ReadJsonAsync(createResponse);
        var roomId = room.GetProperty("id").GetGuid();
        var original = room.GetProperty("services").EnumerateArray().ToArray();
        var projectorId = original.Single(service => service.GetProperty("name").GetString() == "Projector").GetProperty("id").GetGuid();
        var soundId = original.Single(service => service.GetProperty("name").GetString() == "Sound").GetProperty("id").GetGuid();

        using var updateResponse = await admin.PatchAsJsonAsync($"/api/rooms/{roomId}", new
        {
            services = new[]
            {
                new { id = projectorId, name = "Sound", price = 500m },
                new { id = soundId, name = "Projector", price = 700m },
            },
        }, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var getResponse = await admin.GetAsync($"/api/rooms/{roomId}", CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var services = (await ReadJsonAsync(getResponse)).GetProperty("services").EnumerateArray()
            .ToDictionary(service => service.GetProperty("id").GetGuid());
        Assert.Equal("Sound", services[projectorId].GetProperty("name").GetString());
        Assert.Equal(500m, services[projectorId].GetProperty("price").GetDecimal());
        Assert.Equal("Projector", services[soundId].GetProperty("name").GetString());
        Assert.Equal(700m, services[soundId].GetProperty("price").GetDecimal());
    }

    [BusinessFact]
    public async Task AdministratorCanCreateEditAndDeleteRoom()
    {
        using var admin = CreateClient("Admin");
        var room = await CreateRoomAsync(admin);
        var roomId = room.GetProperty("id").GetGuid();
        var serviceId = room.GetProperty("services")[0].GetProperty("id").GetGuid();

        using var patchResponse = await admin.PatchAsJsonAsync($"/api/rooms/{roomId}", new
        {
            capacity = 60,
            baseHourlyRate = 2500m,
            services = new object[]
            {
                new { id = serviceId, name = "Projector", price = 500m },
                new { name = "Sound", price = 700m },
            },
        }, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        var updated = await ReadJsonAsync(patchResponse);
        Assert.Equal(room.GetProperty("name").GetString(), updated.GetProperty("name").GetString());
        Assert.Equal(60, updated.GetProperty("capacity").GetInt32());
        Assert.Equal(2500m, updated.GetProperty("baseHourlyRate").GetDecimal());
        Assert.Equal(2, updated.GetProperty("services").GetArrayLength());
        Assert.Contains(updated.GetProperty("services").EnumerateArray(), service => service.GetProperty("id").GetGuid() == serviceId);

        using var customer = CreateClient();
        using var getResponse = await customer.GetAsync($"/api/rooms/{roomId}", CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(2500m, (await ReadJsonAsync(getResponse)).GetProperty("baseHourlyRate").GetDecimal());

        using var deleteResponse = await admin.DeleteAsync($"/api/rooms/{roomId}", CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        using var deletedResponse = await customer.GetAsync($"/api/rooms/{roomId}", CancellationToken);
        await AssertProblemAsync(deletedResponse, HttpStatusCode.NotFound);

        using var availableResponse = await customer.GetAsync(AvailabilityPath(FutureStart(10), FutureStart(11)), CancellationToken);
        Assert.Equal(HttpStatusCode.OK, availableResponse.StatusCode);
        Assert.DoesNotContain((await ReadJsonAsync(availableResponse)).GetProperty("items").EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == roomId);
    }

    [BusinessFact]
    public async Task OmittedServicesRemainUnchangedAndEmptyListRemovesThem()
    {
        using var admin = CreateClient("Admin");
        var room = await CreateRoomAsync(admin);
        var roomId = room.GetProperty("id").GetGuid();
        var serviceId = room.GetProperty("services")[0].GetProperty("id").GetGuid();

        using var priceResponse = await admin.PatchAsJsonAsync($"/api/rooms/{roomId}", new { baseHourlyRate = 2500m }, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, priceResponse.StatusCode);
        Assert.Equal(serviceId, (await ReadJsonAsync(priceResponse)).GetProperty("services")[0].GetProperty("id").GetGuid());

        using var removalResponse = await admin.PatchAsJsonAsync($"/api/rooms/{roomId}", new { services = Array.Empty<object>() }, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, removalResponse.StatusCode);
        Assert.Empty((await ReadJsonAsync(removalResponse)).GetProperty("services").EnumerateArray());
    }

    [BusinessFact]
    public async Task NullPatchFieldsRetainCurrentValuesWhenAnotherFieldChanges()
    {
        using var admin = CreateClient("Admin");
        var room = await CreateRoomAsync(admin);
        var roomId = room.GetProperty("id").GetGuid();

        using var response = await admin.PatchAsJsonAsync($"/api/rooms/{roomId}", new
        {
            name = (string?)null,
            capacity = (int?)null,
            baseHourlyRate = 2500m,
            services = (object[]?)null,
        }, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await ReadJsonAsync(response);
        Assert.Equal(room.GetProperty("name").GetString(), updated.GetProperty("name").GetString());
        Assert.Equal(room.GetProperty("capacity").GetInt32(), updated.GetProperty("capacity").GetInt32());
        Assert.Equal(2500m, updated.GetProperty("baseHourlyRate").GetDecimal());
        Assert.Equal(room.GetProperty("services")[0].GetProperty("id").GetGuid(), updated.GetProperty("services")[0].GetProperty("id").GetGuid());
    }

    [BusinessFact]
    public async Task EmptyOrAllNullPatchReturnsValidationProblem()
    {
        using var admin = CreateClient("Admin");
        var room = await CreateRoomAsync(admin);
        var roomId = room.GetProperty("id").GetGuid();

        using var emptyResponse = await admin.PatchAsJsonAsync($"/api/rooms/{roomId}", new { }, CancellationToken);
        await AssertProblemAsync(emptyResponse, HttpStatusCode.BadRequest);

        using var nullResponse = await admin.PatchAsJsonAsync($"/api/rooms/{roomId}", new
        {
            name = (string?)null,
            capacity = (int?)null,
            baseHourlyRate = (decimal?)null,
            services = (object[]?)null,
        }, CancellationToken);
        await AssertProblemAsync(nullResponse, HttpStatusCode.BadRequest);
    }

    [BusinessFact]
    public async Task NullServiceListCannotBeUsedWhenCreatingRoom()
    {
        using var admin = CreateClient("Admin");
        using var response = await admin.PostAsJsonAsync("/api/rooms", new
        {
            name = "Room with invalid services",
            capacity = 50,
            baseHourlyRate = 2000m,
            services = (object[]?)null,
        }, CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [BusinessFact]
    public async Task NullServiceItemsAndNamesReturnValidationProblems()
    {
        using var admin = CreateClient("Admin");
        var room = await CreateRoomAsync(admin);
        var roomId = room.GetProperty("id").GetGuid();
        var invalidServices = new object?[][]
        {
            [null],
            [new { name = (string?)null, price = 500m }],
        };

        foreach (var services in invalidServices)
        {
            using var createResponse = await admin.PostAsJsonAsync("/api/rooms", new
            {
                name = "Room with invalid service",
                capacity = 50,
                baseHourlyRate = 2000m,
                services,
            }, CancellationToken);
            await AssertProblemAsync(createResponse, HttpStatusCode.BadRequest);

            using var updateResponse = await admin.PatchAsJsonAsync($"/api/rooms/{roomId}", new { services }, CancellationToken);
            await AssertProblemAsync(updateResponse, HttpStatusCode.BadRequest);
        }
    }

    [BusinessFact]
    public async Task ServiceNamesMustBeUniqueAfterTrimmingAndIgnoringCase()
    {
        using var admin = CreateClient("Admin");
        var room = await CreateRoomAsync(admin);
        var roomId = room.GetProperty("id").GetGuid();
        var services = new[] { new { name = "Projector", price = 500m }, new { name = " projector ", price = 700m } };

        using var createResponse = await admin.PostAsJsonAsync("/api/rooms", new
        {
            name = "Room with duplicate service names",
            capacity = 50,
            baseHourlyRate = 2000m,
            services,
        }, CancellationToken);
        await AssertProblemAsync(createResponse, HttpStatusCode.BadRequest);

        using var updateResponse = await admin.PatchAsJsonAsync($"/api/rooms/{roomId}", new { services }, CancellationToken);
        await AssertProblemAsync(updateResponse, HttpStatusCode.BadRequest);
    }

    [BusinessFact]
    public async Task RepeatingExistingServiceIdentifierReturnsValidationProblem()
    {
        using var admin = CreateClient("Admin");
        var room = await CreateRoomAsync(admin);
        var roomId = room.GetProperty("id").GetGuid();
        var serviceId = room.GetProperty("services")[0].GetProperty("id").GetGuid();

        using var response = await admin.PatchAsJsonAsync($"/api/rooms/{roomId}", new
        {
            services = new[]
            {
                new { id = serviceId, name = "Projector", price = 500m },
                new { id = serviceId, name = "Another service", price = 700m },
            },
        }, CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [BusinessFact]
    public async Task CustomerCannotCreateEditOrDeleteRooms()
    {
        using var admin = CreateClient("Admin");
        var room = await CreateRoomAsync(admin);
        var roomId = room.GetProperty("id").GetGuid();
        using var customer = CreateClient();

        using var createResponse = await customer.PostAsJsonAsync("/api/rooms", new
        {
            name = "Unauthorized room",
            capacity = 20,
            baseHourlyRate = 1000m,
            services = Array.Empty<object>(),
        }, CancellationToken);
        using var updateResponse = await customer.PatchAsJsonAsync($"/api/rooms/{roomId}", new { capacity = 60 }, CancellationToken);
        using var deleteResponse = await customer.DeleteAsync($"/api/rooms/{roomId}", CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [BusinessFact]
    public async Task UnauthenticatedRequestsCannotSearchOrCreateBookings()
    {
        using var anonymous = CreateAnonymousClient();
        using var availableResponse = await anonymous.GetAsync(AvailabilityPath(FutureStart(10), FutureStart(11)), CancellationToken);
        using var bookingResponse = await anonymous.PostAsJsonAsync("/api/bookings", new
        {
            roomId = Guid.NewGuid(),
            startsAt = FutureStart(10),
            durationMinutes = 60,
            serviceIds = Array.Empty<Guid>(),
        }, CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, availableResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, bookingResponse.StatusCode);
    }

    [BusinessFact]
    public async Task InvalidRoomValuesReturnValidationProblems()
    {
        using var admin = CreateClient("Admin");
        var invalidRequests = new[]
        {
            new { name = " ", capacity = 50, baseHourlyRate = 2000m },
            new { name = "Invalid capacity", capacity = 0, baseHourlyRate = 2000m },
            new { name = "Invalid price", capacity = 50, baseHourlyRate = -1m },
            new { name = "Fractional kopeck", capacity = 50, baseHourlyRate = 2000.001m },
        };

        foreach (var request in invalidRequests)
        {
            using var response = await admin.PostAsJsonAsync("/api/rooms", request, CancellationToken);
            await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        }
    }

    [BusinessFact]
    public async Task ForeignServiceCannotBeAttachedDuringRoomUpdate()
    {
        using var admin = CreateClient("Admin");
        var room = await CreateRoomAsync(admin);
        var anotherRoom = await CreateRoomAsync(admin);
        var foreignServiceId = anotherRoom.GetProperty("services")[0].GetProperty("id").GetGuid();

        using var response = await admin.PatchAsJsonAsync($"/api/rooms/{room.GetProperty("id").GetGuid()}", new
        {
            services = new[] { new { id = foreignServiceId, name = "Projector", price = 500m } },
        }, CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [BusinessFact]
    public async Task UnknownRoomsReturnNotFound()
    {
        using var admin = CreateClient("Admin");
        var roomId = Guid.NewGuid();
        using var getResponse = await admin.GetAsync($"/api/rooms/{roomId}", CancellationToken);
        using var updateResponse = await admin.PatchAsJsonAsync($"/api/rooms/{roomId}", new { capacity = 60 }, CancellationToken);
        using var deleteResponse = await admin.DeleteAsync($"/api/rooms/{roomId}", CancellationToken);

        await AssertProblemAsync(getResponse, HttpStatusCode.NotFound);
        await AssertProblemAsync(updateResponse, HttpStatusCode.NotFound);
        await AssertProblemAsync(deleteResponse, HttpStatusCode.NotFound);
    }

    [BusinessFact]
    public async Task AvailabilityRespectsCapacityAndPagination()
    {
        using var admin = CreateClient("Admin");
        var smallRoom = await CreateRoomAsync(admin, capacity: 5);
        await CreateRoomAsync(admin, capacity: 100);
        await CreateRoomAsync(admin, capacity: 100);
        using var customer = CreateClient();
        using var response = await customer.GetAsync(AvailabilityPath(FutureStart(10), FutureStart(11), capacity: 100, pageSize: 1), CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await ReadJsonAsync(response);
        Assert.Equal(1, page.GetProperty("page").GetInt32());
        Assert.Equal(1, page.GetProperty("pageSize").GetInt32());
        Assert.True(page.GetProperty("totalCount").GetInt32() >= 2);
        Assert.Single(page.GetProperty("items").EnumerateArray());
        Assert.All(page.GetProperty("items").EnumerateArray(), item => Assert.True(item.GetProperty("capacity").GetInt32() >= 100));
        Assert.DoesNotContain(page.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == smallRoom.GetProperty("id").GetGuid());

        using var invalidPageResponse = await customer.GetAsync(AvailabilityPath(FutureStart(10), FutureStart(11), pageSize: 1000), CancellationToken);
        await AssertProblemAsync(invalidPageResponse, HttpStatusCode.BadRequest);
    }

    [BusinessFact]
    public async Task AvailabilityTimestampsWithoutExplicitOffsetsAreRejected()
    {
        using var customer = CreateClient();
        var startsWithoutOffset = FutureStart(10).ToString("yyyy-MM-dd'T'HH:mm:ss");
        var endsWithoutOffset = FutureStart(14).ToString("yyyy-MM-dd'T'HH:mm:ss");
        var invalidPeriods = new[]
        {
            (StartsAt: startsWithoutOffset, EndsAt: FutureStart(14).ToString("O")),
            (StartsAt: FutureStart(10).ToString("O"), EndsAt: endsWithoutOffset),
            (StartsAt: startsWithoutOffset, EndsAt: endsWithoutOffset),
        };

        foreach (var period in invalidPeriods)
        {
            var path = $"/api/rooms/available?startsAt={Uri.EscapeDataString(period.StartsAt)}"
                + $"&endsAt={Uri.EscapeDataString(period.EndsAt)}&capacity=50";
            using var response = await customer.GetAsync(path, CancellationToken);
            await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        }
    }
}
