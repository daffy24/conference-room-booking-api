using ConferenceBooking.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ConferenceBooking.Api.AspNetCore.Integration.Tests;

public sealed class BusinessAppFactory : AppFactory, IAsyncLifetime
{
    private const string ConnectionStringVariable = "CONFERENCE_BOOKING_TEST_CONNECTION_STRING";
    private static readonly SemaphoreSlim MigrationLock = new(1, 1);

    public static bool IsConfigured => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionStringVariable));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable)
            ?? throw new InvalidOperationException($"Set {ConnectionStringVariable} to a dedicated PostgreSQL test database.");

        builder.ConfigureTestServices(services =>
        {
            // Keep PostgreSQL semantics and the real model; replace only the database destination.
            services.RemoveAll<DbContextOptions<ConferenceBookingDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ConferenceBookingDbContext>>();
            services.AddDbContext<ConferenceBookingDbContext>(options => options.UseNpgsql(connectionString, postgresql =>
            {
                postgresql.MigrationsAssembly(typeof(Data.Postgresql.DependencyInjection.ServiceCollectionExtensions).Assembly.FullName);
                postgresql.CommandTimeout(15);
                postgresql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), null);
            }));
        });
    }

    public async ValueTask InitializeAsync()
    {
        if (!IsConfigured)
            return;

        // Class fixtures share a database. Serialize schema setup, while test requests remain concurrent.
        await MigrationLock.WaitAsync(TestContext.Current.CancellationToken);
        try
        {
            // The caller owns the disposable database; fixtures never create, drop, or reset it.
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ConferenceBookingDbContext>();
            await dbContext.Database.MigrateAsync(TestContext.Current.CancellationToken);
        }
        finally
        {
            MigrationLock.Release();
        }
    }
}
