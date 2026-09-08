using ConferenceBooking.Api.AspNetCore.Authorization;
using ConferenceBooking.Api.AspNetCore.Modules.Reports.Adapters;
using ConferenceBooking.Api.AspNetCore.Modules.Reports.Models;
using ConferenceBooking.Core.Application.Models;
using ConferenceBooking.Core.Application.Modules.Reports.Models;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceBooking.Api.AspNetCore.Modules.Reports;

/// <summary>Business reports based on confirmed booking prices, accessible only to administrators.</summary>
[ApiController]
[Route("api/reports")]
[Authorize(Policy = AuthorizationPolicies.Admin)]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class ReportsController(ISender sender) : ControllerBase
{
    /// <summary>Returns booked revenue and its daily trend, including days without bookings.</summary>
    /// <remarks>Amounts include future reservations and represent booking value, not received payments.
    /// Dates are inclusive in Europe/Kyiv and refer to the scheduled start, not the creation time.</remarks>
    [HttpGet("revenue")]
    [ProducesResponseType<RevenueReportModel>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RevenueReportModel>> GetRevenue([FromQuery] ReportPeriodModel model,
        [FromServices] IValidator<ReportPeriodModel> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(model, cancellationToken);
        return Ok(await sender.Send(model.ToRevenueRequest(), cancellationToken));
    }

    /// <summary>Ranks rooms by booked revenue and shows booked hours and average booking value.</summary>
    /// <remarks>Active rooms without bookings have zero totals. Removed rooms remain in the report
    /// when they have bookings in the period. Room names are current catalogue labels.</remarks>
    [HttpGet("rooms")]
    [ProducesResponseType<PagedResult<RoomReportModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<RoomReportModel>>> GetRooms([FromQuery] PagedReportModel model,
        [FromServices] IValidator<PagedReportModel> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(model, cancellationToken);
        return Ok(await sender.Send(model.ToRoomRequest(), cancellationToken));
    }

    /// <summary>Ranks services by booking count, then by their booked revenue.</summary>
    /// <remarks>Services are grouped by their stable identifier within a room, including removed services.
    /// Prices come from booking snapshots; the label is the most recently booked name in the period.</remarks>
    [HttpGet("services")]
    [ProducesResponseType<PagedResult<ServiceReportModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ServiceReportModel>>> GetServices([FromQuery] PagedReportModel model,
        [FromServices] IValidator<PagedReportModel> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(model, cancellationToken);
        return Ok(await sender.Send(model.ToServiceRequest(), cancellationToken));
    }
}
