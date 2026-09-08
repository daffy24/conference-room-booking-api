using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConferenceBooking.Data.Postgresql.Migrations;

/// <inheritdoc />
public partial class AddBookingReportIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "ix_bookings_starts_at",
            schema: "public",
            table: "bookings",
            column: "starts_at");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_bookings_starts_at",
            schema: "public",
            table: "bookings");
    }
}
