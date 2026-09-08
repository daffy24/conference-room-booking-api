using ConferenceBooking.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConferenceBooking.Data.Postgresql.Configurations;

internal sealed class BookingEntityConfiguration : IEntityTypeConfiguration<BookingEntity>
{
    public void Configure(EntityTypeBuilder<BookingEntity> entity)
    {
        entity.ToTable("bookings", table =>
        {
            table.HasCheckConstraint("ck_bookings_user_id", "length(btrim(user_id)) > 0");
            table.HasCheckConstraint("ck_bookings_room_name", "length(btrim(room_name)) > 0");
            table.HasCheckConstraint("ck_bookings_interval", "ends_at > starts_at");
            table.HasCheckConstraint("ck_bookings_base_hourly_rate", "base_hourly_rate > 0");
            table.HasCheckConstraint("ck_bookings_costs", "rental_cost >= 0 AND services_cost >= 0 AND total_cost = rental_cost + services_cost");
        });

        entity.Property(x => x.Id).ValueGeneratedNever();
        entity.Property(x => x.UserId).HasMaxLength(200).IsRequired();
        entity.Property(x => x.RoomName).HasMaxLength(100).IsRequired();
        entity.Property(x => x.BaseHourlyRate).HasPrecision(18, 2);
        entity.Property(x => x.RentalCost).HasPrecision(18, 2);
        entity.Property(x => x.ServicesCost).HasPrecision(18, 2);
        entity.Property(x => x.TotalCost).HasPrecision(18, 2);
        entity.Property(x => x.StartsAt).HasColumnType("timestamp with time zone");
        entity.Property(x => x.EndsAt).HasColumnType("timestamp with time zone");
        entity.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");

        entity.HasIndex(x => new { x.RoomId, x.StartsAt })
            .HasDatabaseName("ix_bookings_room_id_starts_at");
        // Cross-room reports filter by time without a leading room identifier.
        entity.HasIndex(x => x.StartsAt).HasDatabaseName("ix_bookings_starts_at");
        entity.HasIndex(x => new { x.UserId, x.CreatedAt })
            .HasDatabaseName("ix_bookings_user_id_created_at");

        entity.HasOne(x => x.Room)
            .WithMany()
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(x => x.Services)
            .WithOne(x => x.Booking)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
