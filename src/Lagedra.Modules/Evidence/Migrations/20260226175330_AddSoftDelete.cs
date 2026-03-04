using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidence.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "evidence",
                table: "uploads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "evidence",
                table: "uploads",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "evidence",
                table: "metadata_stripping_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "evidence",
                table: "metadata_stripping_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "evidence",
                table: "manifests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "evidence",
                table: "manifests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "evidence",
                table: "malware_scan_results",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "evidence",
                table: "malware_scan_results",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "evidence",
                table: "uploads");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "evidence",
                table: "uploads");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "evidence",
                table: "metadata_stripping_logs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "evidence",
                table: "metadata_stripping_logs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "evidence",
                table: "manifests");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "evidence",
                table: "manifests");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "evidence",
                table: "malware_scan_results");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "evidence",
                table: "malware_scan_results");
        }
    }
}
