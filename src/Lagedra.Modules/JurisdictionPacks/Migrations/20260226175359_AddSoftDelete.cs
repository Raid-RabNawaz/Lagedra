using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lagedra.Modules.JurisdictionPacks.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "jurisdiction",
                table: "pack_versions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "jurisdiction",
                table: "pack_versions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "jurisdiction",
                table: "jurisdiction_packs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "jurisdiction",
                table: "jurisdiction_packs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "jurisdiction",
                table: "field_gating_rules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "jurisdiction",
                table: "field_gating_rules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "jurisdiction",
                table: "evidence_schedules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "jurisdiction",
                table: "evidence_schedules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "jurisdiction",
                table: "effective_date_rules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "jurisdiction",
                table: "effective_date_rules",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "jurisdiction",
                table: "pack_versions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "jurisdiction",
                table: "pack_versions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "jurisdiction",
                table: "jurisdiction_packs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "jurisdiction",
                table: "jurisdiction_packs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "jurisdiction",
                table: "field_gating_rules");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "jurisdiction",
                table: "field_gating_rules");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "jurisdiction",
                table: "evidence_schedules");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "jurisdiction",
                table: "evidence_schedules");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "jurisdiction",
                table: "effective_date_rules");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "jurisdiction",
                table: "effective_date_rules");
        }
    }
}
