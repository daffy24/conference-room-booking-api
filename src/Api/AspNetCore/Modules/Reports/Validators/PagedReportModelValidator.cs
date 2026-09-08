using ConferenceBooking.Api.AspNetCore.Modules.Reports.Models;
using FluentValidation;

namespace ConferenceBooking.Api.AspNetCore.Modules.Reports.Validators;

internal sealed class PagedReportModelValidator : AbstractValidator<PagedReportModel>
{
    public PagedReportModelValidator()
    {
        Include(new ReportPeriodModelValidator());
        RuleFor(model => model.Page).InclusiveBetween(1, 1_000_000);
        RuleFor(model => model.PageSize).InclusiveBetween(1, 100);
    }
}
