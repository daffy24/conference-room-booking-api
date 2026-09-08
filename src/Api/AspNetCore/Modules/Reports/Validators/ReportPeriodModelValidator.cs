using ConferenceBooking.Api.AspNetCore.Modules.Reports.Models;
using FluentValidation;

namespace ConferenceBooking.Api.AspNetCore.Modules.Reports.Validators;

internal sealed class ReportPeriodModelValidator : AbstractValidator<ReportPeriodModel>
{
    public ReportPeriodModelValidator()
    {
        RuleFor(model => model.From).NotEmpty();
        RuleFor(model => model.To).NotEmpty().LessThan(DateOnly.MaxValue);
        RuleFor(model => model).Must(model => model.To >= model.From)
            .WithMessage("The last date must be on or after the first date.");
        RuleFor(model => model).Must(model => model.To.DayNumber - model.From.DayNumber < 366)
            .WithMessage("A report period may contain at most 366 calendar days.");
        RuleFor(model => model.RoomId).NotEqual(Guid.Empty).When(model => model.RoomId.HasValue);
    }
}
