using ConferenceBooking.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConferenceBooking.Data.Postgresql.Configurations;

internal sealed class RoomEntityConfiguration : IEntityTypeConfiguration<RoomEntity>
{
    public void Configure(EntityTypeBuilder<RoomEntity> entity)
    {
        entity.ToTable("rooms", table =>
        {
            table.HasCheckConstraint("ck_rooms_name", "length(btrim(name)) > 0");
            table.HasCheckConstraint("ck_rooms_capacity", "capacity > 0");
            table.HasCheckConstraint("ck_rooms_base_hourly_rate", "base_hourly_rate > 0");
        });

        entity.Property(x => x.Id).ValueGeneratedNever();
        entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
        entity.Property(x => x.BaseHourlyRate).HasPrecision(18, 2);
        entity.Property(x => x.Version).IsConcurrencyToken().ValueGeneratedNever();

        entity.HasIndex(x => new { x.Capacity, x.Id })
            .HasDatabaseName("ix_rooms_capacity_id")
            .HasFilter("NOT is_deleted");

        entity.HasMany(x => x.Services)
            .WithOne(x => x.Room)
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        // Room visibility is explicit in queries so historical bookings retain their required room.
    }
}
