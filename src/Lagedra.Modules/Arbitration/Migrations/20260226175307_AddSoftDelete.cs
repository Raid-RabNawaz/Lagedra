using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arbitration.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "arbitration",
                table: "evidence_slots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "arbitration",
                table: "evidence_slots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "arbitration",
                table: "arbitrator_assignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "arbitration",
                table: "arbitrator_assignments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "arbitration",
                table: "arbitration_cases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "arbitration",
                table: "arbitration_cases",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "arbitration",
                table: "evidence_slots");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "arbitration",
                table: "evidence_slots");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "arbitration",
                table: "arbitrator_assignments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "arbitration",
                table: "arbitrator_assignments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "arbitration",
                table: "arbitration_cases");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "arbitration",
                table: "arbitration_cases");
        }
    }
}
