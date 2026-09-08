namespace ConferenceBooking.Api.AspNetCore.Modules.Reports.Models;

/// <summary>A bounded report page for an inclusive range of Kyiv calendar dates.</summary>
public sealed class PagedReportModel : ReportPeriodModel
{
    /// <summary>One-based page number.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Number of rows per page, from 1 to 100.</summary>
    public int PageSize { get; init; } = 20;
}
