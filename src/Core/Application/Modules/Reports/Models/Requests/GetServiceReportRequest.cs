using ConferenceBooking.Core.Application.Models;
using MediatR;

namespace ConferenceBooking.Core.Application.Modules.Reports.Models.Requests;

/// <summary>Ranks booked services by usage and confirmed charges.</summary>
/// <param name="From">The inclusive first date in Kyiv.</param>
/// <param name="To">The inclusive last date in Kyiv.</param>
/// <param name="RoomId">An optional room filter.</param>
/// <param name="Page">The one-based page number.</param>
/// <param name="PageSize">The maximum number of services per page.</param>
public sealed record GetServiceReportRequest(DateOnly From, DateOnly To, Guid? RoomId,
    int Page = 1, int PageSize = 20) : IRequest<PagedResult<ServiceReportModel>>;
