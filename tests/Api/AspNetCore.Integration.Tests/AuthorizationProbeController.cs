using ConferenceBooking.Api.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceBooking.Api.AspNetCore.Integration.Tests;

// Loaded only by the test host; these routes are never included in the deployed API.
[ApiController]
[Route("tests/authorization")]
public sealed class AuthorizationProbeController : ControllerBase
{
    [HttpGet("admin")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public IActionResult Admin() => Ok();

    [HttpGet("customer")]
    [Authorize(Policy = AuthorizationPolicies.Customer)]
    public IActionResult Customer() => Ok();

    [HttpGet("default")]
    public IActionResult Default() => Ok();

    [HttpGet("public")]
    [AllowAnonymous]
    public IActionResult Public() => Ok();
}
