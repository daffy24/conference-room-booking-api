using ConferenceBooking.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConferenceBooking.Data.Postgresql.Configurations;

internal sealed class RoomServiceEntityConfiguration : IEntityTypeConfiguration<RoomServiceEntity>
{
    public void Configure(EntityTypeBuilder<RoomServiceEntity> entity)
    {
        entity.ToTable("room_services", table =>
        {
            table.HasCheckConstraint("ck_room_services_name", "length(btrim(name)) > 0");
            table.HasCheckConstraint("ck_room_services_price", "price >= 0");
        });

        entity.Property(x => x.Id).ValueGeneratedNever();
        entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
        entity.Property(x => x.Price).HasPrecision(18, 2);

        // The migration adds deferred uniqueness so a request may swap two service names atomically.
        // Keeping it outside EF's index model also avoids a circular update-order dependency.
    }
}
