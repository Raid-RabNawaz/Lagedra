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
            // ON CONFLICT DO NOTHING: the row may already exist (created at
            // runtime via the admin settings UI before this migration ran).
            migrationBuilder.Sql(
                """
                INSERT INTO platform.platform_settings ("Key", "Description", "UpdatedAt", "UpdatedByUserId", "Value")
                VALUES ('deposit_return.window_days', 'Days after move-out within which the host must return the deposit or provide an itemized statement of deductions (CA Civil Code §1950.5)', TIMESTAMPTZ '2026-01-01T00:00:00Z', NULL, '21')
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
                keyValue: "deposit_return.window_days");
        }
    }
}
