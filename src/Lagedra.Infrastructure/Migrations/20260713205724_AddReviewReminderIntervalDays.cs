using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lagedra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewReminderIntervalDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ON CONFLICT DO NOTHING: the row may already exist (created at
            // runtime via the admin settings UI before this migration ran).
            migrationBuilder.Sql(
                """
                INSERT INTO platform.platform_settings ("Key", "Description", "UpdatedAt", "UpdatedByUserId", "Value")
                VALUES ('review.reminder_interval_days', 'Days between reminder notifications for parties who have not submitted a stay review', TIMESTAMPTZ '2026-01-01T00:00:00Z', NULL, '3')
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
                keyValue: "review.reminder_interval_days");
        }
    }
}
