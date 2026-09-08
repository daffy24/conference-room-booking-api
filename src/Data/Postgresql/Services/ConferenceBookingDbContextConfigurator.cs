using ConferenceBooking.Data.Abstractions;
using ConferenceBooking.Data.Postgresql.SeedData;
using Microsoft.EntityFrameworkCore;

namespace ConferenceBooking.Data.Postgresql.Services;

internal sealed class ConferenceBookingDbContextConfigurator : IDbContextConfigurator
{
    public void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        // The booking exclusion constraint uses GiST equality support for room identifiers.
        modelBuilder.HasPostgresExtension("btree_gist");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConferenceBookingDbContextConfigurator).Assembly);
        ConferenceRoomSeedData.Configure(modelBuilder);
    }
}
