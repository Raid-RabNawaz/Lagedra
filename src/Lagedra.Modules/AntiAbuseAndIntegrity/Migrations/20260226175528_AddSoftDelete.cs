using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AntiAbuseAndIntegrity.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "integrity",
                table: "fraud_flags",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "integrity",
                table: "fraud_flags",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "integrity",
                table: "collusion_patterns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "integrity",
                table: "collusion_patterns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "integrity",
                table: "account_restrictions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "integrity",
                table: "account_restrictions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "integrity",
                table: "abuse_cases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "integrity",
                table: "abuse_cases",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "integrity",
                table: "fraud_flags");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "integrity",
                table: "fraud_flags");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "integrity",
                table: "collusion_patterns");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "integrity",
                table: "collusion_patterns");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "integrity",
                table: "account_restrictions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "integrity",
                table: "account_restrictions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "integrity",
                table: "abuse_cases");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "integrity",
                table: "abuse_cases");
        }
    }
}
