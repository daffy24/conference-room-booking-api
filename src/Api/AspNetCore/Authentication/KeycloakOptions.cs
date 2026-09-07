using System.ComponentModel.DataAnnotations;

namespace ConferenceBooking.Api.AspNetCore.Authentication;

internal sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    [Required, Url]
    public string Authority { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Required]
    public string SwaggerClientId { get; init; } = string.Empty;

    [Url]
    public string? MetadataAddress { get; init; }
}
