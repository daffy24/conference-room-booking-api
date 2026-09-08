using ConferenceBooking.Core.Application.Modules.Bookings.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ConferenceBooking.Core.Application.DependencyInjection;

/// <summary>
/// Registers application use cases and their dependencies.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers request handlers and the rental calculator.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <returns>The original service collection.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(options =>
            options.RegisterServicesFromAssemblyContaining(typeof(ServiceCollectionExtensions)));
        services.AddSingleton<RentalPriceCalculator>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
