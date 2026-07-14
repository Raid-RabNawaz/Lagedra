using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notifications.Migrations
{
    /// <inheritdoc />
    public partial class AddSmsChannelRecipientAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RecipientEmail",
                schema: "notifications",
                table: "notifications",
                newName: "RecipientAddress");

            migrationBuilder.RenameColumn(
                name: "BrevoMessageId",
                schema: "notifications",
                table: "delivery_logs",
                newName: "ProviderMessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RecipientAddress",
                schema: "notifications",
                table: "notifications",
                newName: "RecipientEmail");

            migrationBuilder.RenameColumn(
                name: "ProviderMessageId",
                schema: "notifications",
                table: "delivery_logs",
                newName: "BrevoMessageId");
        }
    }
}
