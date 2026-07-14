using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lagedra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDepositReturnEvidenceManifest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "platform",
                table: "platform_settings",
                columns: new[] { "Key", "Description", "UpdatedAt", "UpdatedByUserId", "Value" },
                values: new object[] { "deposit_return.window_days", "Days after move-out within which the host must return the deposit or provide an itemized statement of deductions (CA Civil Code §1950.5)", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "21" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "platform",
                table: "platform_settings",
                keyColumn: "Key",
                keyValue: "deposit_return.window_days");
        }
    }
}
