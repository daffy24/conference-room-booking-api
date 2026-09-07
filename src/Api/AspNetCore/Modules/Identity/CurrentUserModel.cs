namespace ConferenceBooking.Api.AspNetCore.Modules.Identity;

/// <summary>
/// Identifies the authenticated user using validated access-token claims.
/// </summary>
/// <param name="Id">The subject identifier within the configured realm.</param>
/// <param name="Username">The user's login name.</param>
/// <param name="Roles">The roles assigned to the user.</param>
public sealed record CurrentUserModel(string Id, string? Username, IReadOnlyList<string> Roles);
