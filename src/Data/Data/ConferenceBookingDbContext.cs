using ConferenceBooking.Data.Abstractions;
using ConferenceBooking.Data.Entities;
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
    /// <summary>
    /// The conference rooms, including rooms retained for booking history.
    /// </summary>
    public DbSet<RoomEntity> Rooms { get; set; } = null!;

    /// <summary>
    /// The services currently offered by rooms.
    /// </summary>
    public DbSet<RoomServiceEntity> RoomServices { get; set; } = null!;

    /// <summary>
    /// The confirmed room bookings.
    /// </summary>
    public DbSet<BookingEntity> Bookings { get; set; } = null!;

    /// <summary>
    /// The service snapshots belonging to bookings.
    /// </summary>
    public DbSet<BookingServiceEntity> BookingServices { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        configurator.OnModelCreating(modelBuilder);
    }
}
