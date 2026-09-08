using ConferenceBooking.Api.AspNetCore.Modules.Reports.Models;
using ConferenceBooking.Core.Application.Modules.Reports.Models.Requests;
using Riok.Mapperly.Abstractions;

namespace ConferenceBooking.Api.AspNetCore.Modules.Reports.Adapters;

[Mapper]
internal static partial class ReportsAdapter
{
    internal static partial GetRevenueReportRequest ToRevenueRequest(this ReportPeriodModel model);

    internal static partial GetRoomReportRequest ToRoomRequest(this PagedReportModel model);

    internal static partial GetServiceReportRequest ToServiceRequest(this PagedReportModel model);
}
