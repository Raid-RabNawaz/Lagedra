using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lagedra.Compliance.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "compliance",
                table: "violations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "compliance",
                table: "violations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "compliance",
                table: "trust_ledger_entries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "compliance",
                table: "trust_ledger_entries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "compliance",
                table: "compliance_signals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "compliance",
                table: "compliance_signals",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "compliance",
                table: "violations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "compliance",
                table: "violations");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "compliance",
                table: "trust_ledger_entries");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "compliance",
                table: "trust_ledger_entries");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "compliance",
                table: "compliance_signals");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "compliance",
                table: "compliance_signals");
        }
    }
}
