using ConferenceBooking.Data.Abstractions;
using ConferenceBooking.Data.Postgresql.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConferenceBooking.Data.Postgresql.DependencyInjection;

/// <summary>
/// Extends <see cref="IServiceCollection"/> with PostgreSQL services.
/// </summary>
public static class ServiceCollectionExtensions
{
    private const string ConnectionStringName = "App";

    /// <summary>
    /// Adds the application database context and its PostgreSQL configuration.
    /// </summary>
    /// <param name="services">The services available in the application.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The original <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("The 'App' connection string is required.");
        }

        services.AddSingleton<IDbContextConfigurator, ConferenceBookingDbContextConfigurator>();
        services.AddDbContext<ConferenceBookingDbContext>(options =>
            options.UseNpgsql(connectionString, postgresql =>
            {
                postgresql.MigrationsAssembly(typeof(ServiceCollectionExtensions).Assembly.FullName);
                postgresql.CommandTimeout(15);
                postgresql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), null);
            }));

        return services;
    }
}
