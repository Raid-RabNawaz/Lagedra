using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lagedra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantServiceFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ON CONFLICT DO NOTHING: rows may already exist (created at
            // runtime via the admin settings UI before this migration ran).
            migrationBuilder.Sql(
                """
                INSERT INTO platform.platform_settings ("Key", "Description", "UpdatedAt", "UpdatedByUserId", "Value")
                VALUES
                    ('service_fee.tenant_bps', 'Tenant platform service fee in basis points of first month''s rent (100 bps = 1%, 0 disables it)', TIMESTAMPTZ '2026-01-01T00:00:00Z', NULL, '0'),
                    ('service_fee.tenant_flat_cents', 'Flat tenant platform service fee in cents (used when service_fee.tenant_use_flat is true)', TIMESTAMPTZ '2026-01-01T00:00:00Z', NULL, '0'),
                    ('service_fee.tenant_use_flat', 'When true, charge a flat tenant service fee (service_fee.tenant_flat_cents); when false, charge a percentage (service_fee.tenant_bps)', TIMESTAMPTZ '2026-01-01T00:00:00Z', NULL, 'false')
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
                keyValue: "service_fee.tenant_bps");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "platform_settings",
                keyColumn: "Key",
                keyValue: "service_fee.tenant_flat_cents");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "platform_settings",
                keyColumn: "Key",
                keyValue: "service_fee.tenant_use_flat");
        }
    }
}
