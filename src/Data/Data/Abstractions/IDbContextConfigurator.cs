using Microsoft.EntityFrameworkCore;

namespace ConferenceBooking.Data.Abstractions;

/// <summary>
/// Configures the database model for a specific provider.
/// </summary>
public interface IDbContextConfigurator
{
    /// <summary>
    /// Applies entity configurations to the database model.
    /// </summary>
    /// <param name="modelBuilder">The database model builder.</param>
    void OnModelCreating(ModelBuilder modelBuilder);
}
