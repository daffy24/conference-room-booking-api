using MediatR;

namespace ConferenceBooking.Core.Application.Modules.Reports.Models.Requests;

/// <summary>Summarizes confirmed booking value by the local booking start date.</summary>
/// <param name="From">The inclusive first date in Kyiv.</param>
/// <param name="To">The inclusive last date in Kyiv.</param>
/// <param name="RoomId">An optional room filter.</param>
public sealed record GetRevenueReportRequest(DateOnly From, DateOnly To, Guid? RoomId)
    : IRequest<RevenueReportModel>;
