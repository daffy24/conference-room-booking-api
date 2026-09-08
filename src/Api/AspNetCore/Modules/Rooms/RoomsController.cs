using ConferenceBooking.Api.AspNetCore.Authorization;
using ConferenceBooking.Api.AspNetCore.Modules.Rooms.AddRoom;
using ConferenceBooking.Api.AspNetCore.Modules.Rooms.SearchAvailableRooms;
using ConferenceBooking.Api.AspNetCore.Modules.Rooms.UpdateRoom;
using ConferenceBooking.Core.Application.Models;
using ConferenceBooking.Core.Application.Modules.Rooms.Models;
using ConferenceBooking.Core.Application.Modules.Rooms.Models.Requests;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceBooking.Api.AspNetCore.Modules.Rooms;

/// <summary>Room management and availability search.</summary>
[ApiController]
[Route("api/rooms")]
[Authorize]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public sealed class RoomsController(ISender sender) : ControllerBase
{
    /// <summary>Creates a room with its available services. Requires the Admin role.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [ProducesResponseType<RoomModel>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RoomModel>> Add([FromBody] AddRoomModel model,
        [FromServices] IValidator<AddRoomModel> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(model, cancellationToken);
        var room = await sender.Send(model.ToRequest(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = room.Id }, room);
    }

    /// <summary>Returns a room and its current service prices.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<RoomModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoomModel>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetRoomRequest(id), cancellationToken));

    /// <summary>Updates supplied room fields. A supplied service list replaces the existing list.</summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [ProducesResponseType<RoomModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoomModel>> Update(Guid id, [FromBody] UpdateRoomModel model,
        [FromServices] IValidator<UpdateRoomModel> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(model, cancellationToken);
        return Ok(await sender.Send(model.ToRequest(id), cancellationToken));
    }

    /// <summary>Removes a room from the catalogue, retaining historical bookings.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteRoomRequest(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Finds available rooms during 06:00–23:00 Kyiv time, ordered by capacity.</summary>
    [HttpGet("available")]
    [ProducesResponseType<PagedResult<RoomModel>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<RoomModel>>> SearchAvailable(
        [FromQuery] SearchAvailableRoomsModel model,
        [FromServices] IValidator<SearchAvailableRoomsModel> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(model, cancellationToken);
        return Ok(await sender.Send(model.ToRequest(), cancellationToken));
    }
}
