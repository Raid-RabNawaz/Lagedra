using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lagedra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewWindowDaysSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ON CONFLICT DO NOTHING: the row may already exist (created at
            // runtime via the admin settings UI before this migration ran).
            migrationBuilder.Sql(
                """
                INSERT INTO platform.platform_settings ("Key", "Description", "UpdatedAt", "UpdatedByUserId", "Value")
                VALUES ('review.window_days', 'Days after stay completion within which host and guest may submit double-blind reviews', TIMESTAMPTZ '2026-01-01T00:00:00Z', NULL, '14')
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
                keyValue: "review.window_days");
        }
    }
}
