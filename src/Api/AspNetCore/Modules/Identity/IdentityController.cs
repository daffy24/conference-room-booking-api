using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceBooking.Api.AspNetCore.Modules.Identity;

/// <summary>
/// Provides information about the authenticated caller.
/// </summary>
[ApiController]
[Route("api/identity")]
[Authorize]
public sealed class IdentityController : ControllerBase
{
    /// <summary>
    /// Returns the identity established by access-token validation.
    /// </summary>
    /// <returns>The current user's identifier, name, and roles.</returns>
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<CurrentUserModel> GetCurrentUser() =>
        new CurrentUserModel(User.FindFirstValue("sub")!, User.Identity?.Name,
            [.. User.FindAll("roles").Select(claim => claim.Value).Distinct()]);
}
