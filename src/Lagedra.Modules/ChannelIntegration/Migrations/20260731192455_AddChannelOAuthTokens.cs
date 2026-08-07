using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChannelIntegration.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelOAuthTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncryptedRefreshToken",
                schema: "channel_integration",
                table: "channel_connections",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TokenExpiresAt",
                schema: "channel_integration",
                table: "channel_connections",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedRefreshToken",
                schema: "channel_integration",
                table: "channel_connections");

            migrationBuilder.DropColumn(
                name: "TokenExpiresAt",
                schema: "channel_integration",
                table: "channel_connections");
        }
    }
}
