using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Lagedra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformSettings : Migration
    {
        private static readonly string[] platform_settings = new[] { "Key", "Description", "UpdatedAt", "UpdatedByUserId", "Value" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "platform");

            migrationBuilder.CreateTable(
                name: "platform_settings",
                schema: "platform",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_settings", x => x.Key);
                });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "platform_settings",
                columns: platform_settings,
                values: new object[,]
                {
                    { "arbitration_fee.binding_arbitration_cents", "Binding arbitration filing fee in cents", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "9900" },
                    { "arbitration_fee.protocol_adjudication_cents", "Protocol adjudication filing fee in cents", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "4900" },
                    { "protocol_fee.monthly_cents", "Monthly protocol fee per active deal in cents (paid by host)", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "7900" },
                    { "protocol_fee.pilot_active", "Whether the pilot discount is currently active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "false" },
                    { "protocol_fee.pilot_discount_cents", "Pilot discount in cents subtracted from protocol fee", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "3900" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "platform_settings",
                schema: "platform");
        }
    }
}
