using ConferenceBooking.Api.AspNetCore.Authentication;
using ConferenceBooking.Api.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ConferenceBooking.Api.AspNetCore.DependencyInjection;

internal static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOptions<KeycloakOptions>()
            .Bind(configuration.GetRequiredSection(KeycloakOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => environment.IsDevelopment() ||
                (IsHttps(options.Authority) && (options.MetadataAddress is null || IsHttps(options.MetadataAddress))),
                "Keycloak URLs must use HTTPS outside Development.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<KeycloakOptions>>((options, keycloakOptions) =>
            {
                var keycloak = keycloakOptions.Value;
                options.Authority = keycloak.Authority;
                if (keycloak.MetadataAddress is not null)
                {
                    options.MetadataAddress = keycloak.MetadataAddress;
                }
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.MapInboundClaims = false;
                options.IncludeErrorDetails = false;
                options.BackchannelTimeout = TimeSpan.FromSeconds(10);
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = keycloak.Authority,
                    ValidateAudience = true,
                    ValidAudience = keycloak.Audience,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidateIssuerSigningKey = true,
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "preferred_username",
                    RoleClaimType = "roles",
                };
            });

        var authenticatedUser = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim("sub")
            .Build();

        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(authenticatedUser)
            .SetFallbackPolicy(authenticatedUser)
            .AddPolicy(AuthorizationPolicies.Admin, policy => policy
                .Combine(authenticatedUser)
                .RequireRole("Admin"))
            .AddPolicy(AuthorizationPolicies.Customer, policy => policy
                .Combine(authenticatedUser)
                .RequireRole("Customer", "Admin"));

        return services;
    }

    private static bool IsHttps(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}
