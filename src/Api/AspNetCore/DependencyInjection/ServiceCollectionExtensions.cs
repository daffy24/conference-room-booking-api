using ConferenceBooking.Api.AspNetCore.OpenApi;
using ConferenceBooking.Data;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.OpenApi;

namespace ConferenceBooking.Api.AspNetCore.DependencyInjection;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.Configure<KestrelServerOptions>(options =>
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = 64 * 1024;
        });
        services.AddControllers();
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Conference Booking API",
                    Version = "v1",
                    Description = "Application foundation. Business endpoints will be added incrementally.",
                };

                return Task.CompletedTask;
            });
            options.AddDocumentTransformer<AuthenticationDocumentTransformer>();
        });
        services.AddHealthChecks()
            .AddDbContextCheck<ConferenceBookingDbContext>("postgresql", tags: ["ready"]);

        return services;
    }
}
