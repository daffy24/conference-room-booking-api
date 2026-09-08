using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ConferenceBooking.Data.Postgresql.Migrations;

/// <inheritdoc />
public partial class InitialBookingSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "public");

        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:PostgresExtension:btree_gist", ",,");

        migrationBuilder.CreateTable(
            name: "rooms",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                capacity = table.Column<int>(type: "integer", nullable: false),
                base_hourly_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                version = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_rooms", x => x.id);
                table.CheckConstraint("ck_rooms_base_hourly_rate", "base_hourly_rate > 0");
                table.CheckConstraint("ck_rooms_capacity", "capacity > 0");
                table.CheckConstraint("ck_rooms_name", "length(btrim(name)) > 0");
            });

        migrationBuilder.CreateTable(
            name: "bookings",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                room_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                room_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                base_hourly_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                rental_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                services_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                total_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_bookings", x => x.id);
                table.CheckConstraint("ck_bookings_base_hourly_rate", "base_hourly_rate > 0");
                table.CheckConstraint("ck_bookings_costs", "rental_cost >= 0 AND services_cost >= 0 AND total_cost = rental_cost + services_cost");
                table.CheckConstraint("ck_bookings_interval", "ends_at > starts_at");
                table.CheckConstraint("ck_bookings_room_name", "length(btrim(room_name)) > 0");
                table.CheckConstraint("ck_bookings_user_id", "length(btrim(user_id)) > 0");
                table.ForeignKey(
                    name: "FK_bookings_rooms_room_id",
                    column: x => x.room_id,
                    principalSchema: "public",
                    principalTable: "rooms",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "room_services",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                room_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_room_services", x => x.id);
                table.CheckConstraint("ck_room_services_name", "length(btrim(name)) > 0");
                table.CheckConstraint("ck_room_services_price", "price >= 0");
                table.ForeignKey(
                    name: "FK_room_services_rooms_room_id",
                    column: x => x.room_id,
                    principalSchema: "public",
                    principalTable: "rooms",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "booking_services",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                service_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_booking_services", x => x.id);
                table.CheckConstraint("ck_booking_services_name", "length(btrim(name)) > 0");
                table.CheckConstraint("ck_booking_services_price", "price >= 0");
                table.ForeignKey(
                    name: "FK_booking_services_bookings_booking_id",
                    column: x => x.booking_id,
                    principalSchema: "public",
                    principalTable: "bookings",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        // PostgreSQL enforces this across every API instance; adjacent reservations may share an endpoint.
        migrationBuilder.Sql("""
            ALTER TABLE public.bookings
            ADD CONSTRAINT ex_bookings_room_period
            EXCLUDE USING gist (
                room_id WITH =,
                tstzrange(starts_at, ends_at, '[)') WITH &&
            );
            """);

        // Check the final transaction state so valid renames can exchange existing service names.
        migrationBuilder.Sql("""
            ALTER TABLE public.room_services
            ADD CONSTRAINT ux_room_services_room_id_name
            UNIQUE (room_id, name) DEFERRABLE INITIALLY DEFERRED;
            """);

        migrationBuilder.InsertData(
            schema: "public",
            table: "rooms",
            columns: new[] { "id", "base_hourly_rate", "capacity", "is_deleted", "name", "version" },
            values: new object[,]
            {
                { new Guid("10000000-0000-0000-0000-000000000001"), 2000m, 50, false, "Зал А", new Guid("10000000-0000-0000-0000-000000000001") },
                { new Guid("10000000-0000-0000-0000-000000000002"), 3500m, 100, false, "Зал B", new Guid("10000000-0000-0000-0000-000000000002") },
                { new Guid("10000000-0000-0000-0000-000000000003"), 1500m, 30, false, "Зал C", new Guid("10000000-0000-0000-0000-000000000003") }
            });

        migrationBuilder.InsertData(
            schema: "public",
            table: "room_services",
            columns: new[] { "id", "name", "price", "room_id" },
            values: new object[,]
            {
                { new Guid("20000000-0000-0000-0000-000000000011"), "Проєктор", 500m, new Guid("10000000-0000-0000-0000-000000000001") },
                { new Guid("20000000-0000-0000-0000-000000000012"), "Wi-Fi", 300m, new Guid("10000000-0000-0000-0000-000000000001") },
                { new Guid("20000000-0000-0000-0000-000000000013"), "Звук", 700m, new Guid("10000000-0000-0000-0000-000000000001") },
                { new Guid("20000000-0000-0000-0000-000000000021"), "Проєктор", 500m, new Guid("10000000-0000-0000-0000-000000000002") },
                { new Guid("20000000-0000-0000-0000-000000000022"), "Wi-Fi", 300m, new Guid("10000000-0000-0000-0000-000000000002") },
                { new Guid("20000000-0000-0000-0000-000000000023"), "Звук", 700m, new Guid("10000000-0000-0000-0000-000000000002") },
                { new Guid("20000000-0000-0000-0000-000000000031"), "Проєктор", 500m, new Guid("10000000-0000-0000-0000-000000000003") },
                { new Guid("20000000-0000-0000-0000-000000000032"), "Wi-Fi", 300m, new Guid("10000000-0000-0000-0000-000000000003") },
                { new Guid("20000000-0000-0000-0000-000000000033"), "Звук", 700m, new Guid("10000000-0000-0000-0000-000000000003") }
            });

        migrationBuilder.CreateIndex(
            name: "ux_booking_services_booking_id_service_id",
            schema: "public",
            table: "booking_services",
            columns: new[] { "booking_id", "service_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_bookings_room_id_starts_at",
            schema: "public",
            table: "bookings",
            columns: new[] { "room_id", "starts_at" });

        migrationBuilder.CreateIndex(
            name: "ix_bookings_user_id_created_at",
            schema: "public",
            table: "bookings",
            columns: new[] { "user_id", "created_at" });

        migrationBuilder.CreateIndex(
            name: "IX_room_services_room_id",
            schema: "public",
            table: "room_services",
            column: "room_id");

        migrationBuilder.CreateIndex(
            name: "ix_rooms_capacity_id",
            schema: "public",
            table: "rooms",
            columns: new[] { "capacity", "id" },
            filter: "NOT is_deleted");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "booking_services",
            schema: "public");

        migrationBuilder.DropTable(
            name: "room_services",
            schema: "public");

        migrationBuilder.DropTable(
            name: "bookings",
            schema: "public");

        migrationBuilder.DropTable(
            name: "rooms",
            schema: "public");
    }
}
