using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Privacy.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "privacy",
                table: "user_consents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "privacy",
                table: "user_consents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "privacy",
                table: "legal_holds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "privacy",
                table: "legal_holds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "privacy",
                table: "deletion_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "privacy",
                table: "deletion_requests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "privacy",
                table: "data_export_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "privacy",
                table: "data_export_requests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "privacy",
                table: "consent_records",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "privacy",
                table: "consent_records",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "privacy",
                table: "user_consents");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "privacy",
                table: "user_consents");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "privacy",
                table: "legal_holds");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "privacy",
                table: "legal_holds");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "privacy",
                table: "deletion_requests");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "privacy",
                table: "deletion_requests");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "privacy",
                table: "data_export_requests");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "privacy",
                table: "data_export_requests");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "privacy",
                table: "consent_records");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "privacy",
                table: "consent_records");
        }
    }
}
