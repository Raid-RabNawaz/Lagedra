using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListingAndLocation.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateListingAndLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "listings");

            migrationBuilder.CreateTable(
                name: "listings",
                schema: "listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LandlordUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    MonthlyRentCents = table.Column<long>(type: "bigint", nullable: false),
                    InsuranceRequired = table.Column<bool>(type: "boolean", nullable: false),
                    stay_min_days = table.Column<int>(type: "integer", nullable: true),
                    stay_max_days = table.Column<int>(type: "integer", nullable: true),
                    approx_latitude = table.Column<double>(type: "double precision", nullable: true),
                    approx_longitude = table.Column<double>(type: "double precision", nullable: true),
                    precise_street = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    precise_city = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    precise_state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    precise_zip_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    precise_country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    JurisdictionCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_listings_LandlordUserId",
                schema: "listings",
                table: "listings",
                column: "LandlordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_listings_Status",
                schema: "listings",
                table: "listings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt",
                schema: "listings",
                table: "outbox_messages",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "listings",
                schema: "listings");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "listings");
        }
    }
}
