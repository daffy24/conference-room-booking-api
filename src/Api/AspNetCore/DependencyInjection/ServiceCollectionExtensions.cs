using ConferenceBooking.Api.AspNetCore.Binding;
using ConferenceBooking.Api.AspNetCore.Exceptions;
using ConferenceBooking.Api.AspNetCore.OpenApi;
using ConferenceBooking.Api.AspNetCore.Serialization;
using ConferenceBooking.Data;
using FluentValidation;
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
        services.AddControllers(options => options.ModelBinderProviders.Insert(0, new DateTimeOffsetModelBinderProvider()))
            .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new DateTimeOffsetJsonConverter());
            options.JsonSerializerOptions.RespectNullableAnnotations = true;
            options.JsonSerializerOptions.UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow;
        });
        services.AddValidatorsFromAssembly(typeof(ServiceCollectionExtensions).Assembly, includeInternalTypes: true);
        services.AddExceptionHandler<ApiExceptionHandler>();
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
                    Description = "Conference rooms, availability and bookings. Prices are in UAH; business hours use Europe/Kyiv.",
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
