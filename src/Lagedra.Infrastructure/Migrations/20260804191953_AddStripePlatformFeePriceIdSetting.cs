using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lagedra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStripePlatformFeePriceIdSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ON CONFLICT DO NOTHING: the row may already exist (created at
            // runtime via the admin settings UI before this migration ran) —
            // a plain INSERT crashed the staging API on startup (2026-08-08).
            migrationBuilder.Sql(
                """
                INSERT INTO platform.platform_settings ("Key", "Description", "UpdatedAt", "UpdatedByUserId", "Value")
                VALUES ('stripe.platform_fee_price_id', 'Stripe Price ID (price_…) for the host monthly protocol fee subscription', TIMESTAMPTZ '2026-01-01T00:00:00Z', NULL, '')
                ON CONFLICT ("Key") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "platform",
                table: "platform_settings",
                keyColumn: "Key",
                keyValue: "stripe.platform_fee_price_id");
        }
    }
}
