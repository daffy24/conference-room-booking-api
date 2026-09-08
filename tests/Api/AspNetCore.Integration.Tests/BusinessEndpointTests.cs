using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ConferenceBooking.Api.AspNetCore.Integration.Tests;

public abstract class BusinessEndpointTests(BusinessAppFactory factory)
{
    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    protected IServiceProvider Services => factory.Services;

    protected HttpClient CreateAnonymousClient() => factory.CreateClient();

    protected HttpClient CreateClient(string role = "Customer", string subject = "user-123")
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken(role, subject: subject));
        return client;
    }

    protected static async Task<JsonElement> CreateRoomAsync(HttpClient client, int capacity = 50)
    {
        using var response = await client.PostAsJsonAsync("/api/rooms", new
        {
            name = $"Integration room {Guid.NewGuid():N}",
            capacity,
            baseHourlyRate = 2000m,
            services = new[] { new { name = "Projector", price = 500m } },
        }, CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var room = await ReadJsonAsync(response);
        Assert.NotEqual(Guid.Empty, room.GetProperty("id").GetGuid());
        return room;
    }

    protected static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = await response.Content.ReadFromJsonAsync<JsonDocument>(CancellationToken);
        Assert.NotNull(document);
        return document.RootElement.Clone();
    }

    protected static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await ReadJsonAsync(response);
        Assert.Equal((int)status, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("title").GetString()));
    }

    protected static DateTimeOffset FutureStart(int hour, int minute = 0) =>
        new(DateTimeOffset.UtcNow.Year + 1, 1, 2, hour, minute, 0, TimeSpan.FromHours(2));

    protected static string AvailabilityPath(DateTimeOffset startsAt, DateTimeOffset endsAt, int capacity = 50, int pageSize = 100) =>
        $"/api/rooms/available?startsAt={Uri.EscapeDataString(startsAt.ToString("O"))}"
        + $"&endsAt={Uri.EscapeDataString(endsAt.ToString("O"))}&capacity={capacity}&page=1&pageSize={pageSize}";
}
