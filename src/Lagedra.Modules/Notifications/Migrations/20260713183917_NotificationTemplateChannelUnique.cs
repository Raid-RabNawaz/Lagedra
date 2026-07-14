using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notifications.Migrations
{
    /// <inheritdoc />
    public partial class NotificationTemplateChannelUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notification_templates_TemplateId",
                schema: "notifications",
                table: "notification_templates");

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_TemplateId_Channel",
                schema: "notifications",
                table: "notification_templates",
                columns: new[] { "TemplateId", "Channel" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notification_templates_TemplateId_Channel",
                schema: "notifications",
                table: "notification_templates");

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_TemplateId",
                schema: "notifications",
                table: "notification_templates",
                column: "TemplateId",
                unique: true);
        }
    }
}
