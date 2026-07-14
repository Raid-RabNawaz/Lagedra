using System;
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
            migrationBuilder.InsertData(
                schema: "platform",
                table: "platform_settings",
                columns: new[] { "Key", "Description", "UpdatedAt", "UpdatedByUserId", "Value" },
                values: new object[] { "review.window_days", "Days after stay completion within which host and guest may submit double-blind reviews", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "14" });
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
