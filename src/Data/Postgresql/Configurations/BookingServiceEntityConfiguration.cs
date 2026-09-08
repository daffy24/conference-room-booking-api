using ConferenceBooking.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConferenceBooking.Data.Postgresql.Configurations;

internal sealed class BookingServiceEntityConfiguration : IEntityTypeConfiguration<BookingServiceEntity>
{
    public void Configure(EntityTypeBuilder<BookingServiceEntity> entity)
    {
        entity.ToTable("booking_services", table =>
        {
            table.HasCheckConstraint("ck_booking_services_name", "length(btrim(name)) > 0");
            table.HasCheckConstraint("ck_booking_services_price", "price >= 0");
        });

        entity.Property(x => x.Id).ValueGeneratedNever();
        entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
        entity.Property(x => x.Price).HasPrecision(18, 2);

        entity.HasIndex(x => new { x.BookingId, x.ServiceId })
            .IsUnique()
            .HasDatabaseName("ux_booking_services_booking_id_service_id");

        // ServiceId is intentionally a snapshot, not a foreign key to an editable room service.
    }
}
