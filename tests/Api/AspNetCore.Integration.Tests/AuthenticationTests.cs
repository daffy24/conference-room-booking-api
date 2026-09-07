using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ConferenceBooking.Api.AspNetCore.Modules.Identity;

namespace ConferenceBooking.Api.AspNetCore.Integration.Tests;

public sealed class AuthenticationTests(AppFactory factory) : IClassFixture<AppFactory>
{
    [Fact]
    public async Task CurrentUserWithoutTokenReturnsChallenge()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/identity/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, value => value.Scheme == "Bearer");
    }

    [Fact]
    public async Task CurrentUserComesFromValidatedClaims()
    {
        using var client = CreateClient(factory.CreateToken());
        var user = await client.GetFromJsonAsync<CurrentUserModel>("/api/identity/me", TestContext.Current.CancellationToken);

        Assert.NotNull(user);
        Assert.Equal("user-123", user.Id);
        Assert.Equal("test-customer", user.Username);
        Assert.Equal(["Customer"], user.Roles);
    }

    [Theory]
    [InlineData("signature")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("expiration")]
    [InlineData("not-before")]
    [InlineData("unsigned")]
    public async Task InvalidTokenIsRejected(string invalidPart)
    {
        using var client = CreateClient(factory.CreateToken(invalidPart: invalidPart));
        using var response = await client.GetAsync("/api/identity/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain("IDX", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MissingSubjectIsRejected()
    {
        using var client = CreateClient(factory.CreateToken(invalidPart: "subject"));
        using var response = await client.GetAsync("/api/identity/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("Admin", "admin", HttpStatusCode.OK)]
    [InlineData("Admin", "customer", HttpStatusCode.OK)]
    [InlineData("Customer", "customer", HttpStatusCode.OK)]
    [InlineData("Customer", "admin", HttpStatusCode.Forbidden)]
    [InlineData(null, "admin", HttpStatusCode.Forbidden)]
    [InlineData(null, "customer", HttpStatusCode.Forbidden)]
    public async Task PoliciesEnforceRoles(string? role, string policy, HttpStatusCode expectedStatus)
    {
        using var client = CreateClient(factory.CreateToken(role));
        using var response = await client.GetAsync($"/tests/authorization/{policy}", TestContext.Current.CancellationToken);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Fact]
    public async Task EndpointWithoutExplicitPolicyIsProtected()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/tests/authorization/default", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/swagger/index.html")]
    [InlineData("/tests/authorization/public")]
    public async Task PublicEndpointRemainsAccessible(string path)
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiDocumentsCodeFlowAndPublicExceptions()
    {
        using var client = factory.CreateClient();
        using var document = await client.GetFromJsonAsync<JsonDocument>("/openapi/v1.json", TestContext.Current.CancellationToken);
        Assert.NotNull(document);
        var root = document.RootElement;
        var flow = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Keycloak")
            .GetProperty("flows").GetProperty("authorizationCode");

        Assert.Equal("http://localhost:8080/realms/conference-booking/protocol/openid-connect/auth",
            flow.GetProperty("authorizationUrl").GetString());
        Assert.Equal(0, root.GetProperty("paths").GetProperty("/tests/authorization/public")
            .GetProperty("get").GetProperty("security").GetArrayLength());
        Assert.Equal(1, root.GetProperty("security").GetArrayLength());
    }

    private HttpClient CreateClient(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
