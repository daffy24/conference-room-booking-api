using ConferenceBooking.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ConferenceBooking.Data;

/// <summary>
/// Provides access to application entities and tracks changes within a request.
/// </summary>
/// <param name="options">The database context options.</param>
/// <param name="configurator">The provider-specific model configurator.</param>
public sealed class ConferenceBookingDbContext(
    DbContextOptions<ConferenceBookingDbContext> options,
    IDbContextConfigurator configurator
) : DbContext(options)
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        configurator.OnModelCreating(modelBuilder);
    }
}
