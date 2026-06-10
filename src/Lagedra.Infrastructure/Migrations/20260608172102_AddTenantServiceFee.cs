using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Lagedra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantServiceFee : Migration
    {
        private static readonly string[] platform_settings = new[] { "Key", "Description", "UpdatedAt", "UpdatedByUserId", "Value" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "platform",
                table: "platform_settings",
                columns: platform_settings,
                values: new object[,]
                {
                    { "service_fee.tenant_bps", "Tenant platform service fee in basis points of first month's rent (100 bps = 1%, 0 disables it)", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "0" },
                    { "service_fee.tenant_flat_cents", "Flat tenant platform service fee in cents (used when service_fee.tenant_use_flat is true)", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "0" },
                    { "service_fee.tenant_use_flat", "When true, charge a flat tenant service fee (service_fee.tenant_flat_cents); when false, charge a percentage (service_fee.tenant_bps)", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "false" }
                });
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
