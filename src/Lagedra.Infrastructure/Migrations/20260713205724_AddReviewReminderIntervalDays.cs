using System;
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
            migrationBuilder.InsertData(
                schema: "platform",
                table: "platform_settings",
                columns: new[] { "Key", "Description", "UpdatedAt", "UpdatedByUserId", "Value" },
                values: new object[] { "review.reminder_interval_days", "Days between reminder notifications for parties who have not submitted a stay review", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "3" });
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
