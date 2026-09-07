using ConferenceBooking.Api.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace ConferenceBooking.Api.AspNetCore.OpenApi;

internal sealed class AuthenticationDocumentTransformer(IOptions<KeycloakOptions> keycloakOptions)
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var authority = keycloakOptions.Value.Authority.TrimEnd('/');
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Keycloak"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"{authority}/protocol/openid-connect/auth"),
                    TokenUrl = new Uri($"{authority}/protocol/openid-connect/token"),
                    Scopes = new Dictionary<string, string> { ["openid"] = "Sign in to Conference Booking" },
                },
            },
        };

        document.Security =
        [
            new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Keycloak", document)] = ["openid"] },
        ];

        // Public operations override the document's authenticated-by-default requirement.
        foreach (var description in context.DescriptionGroups.SelectMany(group => group.Items)
                     .Where(description => description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any()))
        {
            if (!document.Paths.TryGetValue($"/{description.RelativePath}", out var path) ||
                path.Operations is null) continue;
            foreach (var operation in path.Operations.Where(operation =>
                         string.Equals(operation.Key.Method, description.HttpMethod, StringComparison.OrdinalIgnoreCase)))
            {
                operation.Value.Security = [];
            }
        }

        return Task.CompletedTask;
    }
}
