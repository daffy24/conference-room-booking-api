using ConferenceBooking.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ConferenceBooking.Data.Postgresql.Services;

internal sealed class ConferenceBookingDbContextConfigurator : IDbContextConfigurator
{
    public void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.HasDefaultSchema("public");
}
