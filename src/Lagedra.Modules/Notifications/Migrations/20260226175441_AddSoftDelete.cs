using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notifications.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "notifications",
                table: "user_notification_preferences",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "notifications",
                table: "user_notification_preferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "notifications",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "notifications",
                table: "notifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "notifications",
                table: "notification_templates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "notifications",
                table: "notification_templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "notifications",
                table: "delivery_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "notifications",
                table: "delivery_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "notifications",
                table: "user_notification_preferences");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "notifications",
                table: "user_notification_preferences");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "notifications",
                table: "notification_templates");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "notifications",
                table: "notification_templates");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "notifications",
                table: "delivery_logs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "notifications",
                table: "delivery_logs");
        }
    }
}
