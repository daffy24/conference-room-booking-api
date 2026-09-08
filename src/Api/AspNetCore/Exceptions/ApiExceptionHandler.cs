using ConferenceBooking.Core.Application.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ConferenceBooking.Api.AspNetCore.Exceptions;

internal sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = CreateProblem(exception);
        if (problem is null)
            return false;

        httpContext.Response.StatusCode = problem.Status!.Value;
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
        return true;
    }

    private static ProblemDetails? CreateProblem(Exception exception) => exception switch
    {
        ValidationException validation => new ValidationProblemDetails(validation.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).Distinct().ToArray()))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed.",
        },
        NotFoundException => Problem(StatusCodes.Status404NotFound, "Resource not found.", exception.Message),
        ConflictException => Problem(StatusCodes.Status409Conflict, "Conflict.", exception.Message),
        DbUpdateConcurrencyException => Problem(StatusCodes.Status409Conflict, "Concurrent modification.",
            "The room changed while the request was being processed. Refresh its details and try again."),
        DbUpdateException
        {
            InnerException: PostgresException
            {
                SqlState: PostgresErrorCodes.ExclusionViolation,
                ConstraintName: "ex_bookings_room_period"
            }
        } => Problem(StatusCodes.Status409Conflict, "Room unavailable.",
            "The room is already booked for this period."),
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } }
            => Problem(StatusCodes.Status409Conflict, "Conflicting values.", "A record with these unique values already exists."),
        _ => null,
    };

    private static ProblemDetails Problem(int status, string title, string detail)
        => new() { Status = status, Title = title, Detail = detail };
}
