using System.Security.Claims;
using ConferenceBooking.Api.AspNetCore.Authorization;
using ConferenceBooking.Api.AspNetCore.Modules.Bookings.AddBooking;
using ConferenceBooking.Core.Application.Modules.Bookings.Models;
using ConferenceBooking.Core.Application.Modules.Bookings.Models.Requests;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceBooking.Api.AspNetCore.Modules.Bookings;

/// <summary>Reservations with server-calculated price snapshots.</summary>
[ApiController]
[Route("api/bookings")]
[Authorize]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public sealed class BookingsController(ISender sender) : ControllerBase
{
    /// <summary>Reserves a room. Each tariff segment is priced separately and services are charged once.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.Customer)]
    [ProducesResponseType<BookingModel>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingModel>> Add([FromBody] AddBookingModel model,
        [FromServices] IValidator<AddBookingModel> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(model, cancellationToken);
        // Ownership comes exclusively from the validated token, never from request data.
        var booking = await sender.Send(model.ToRequest(User.FindFirstValue("sub")!), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = booking.Id }, booking);
    }

    /// <summary>Returns a booking for its owner or an administrator.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<BookingModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingModel>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var request = new GetBookingRequest(id, User.FindFirstValue("sub")!, User.IsInRole("Admin"));
        return Ok(await sender.Send(request, cancellationToken));
    }
}
