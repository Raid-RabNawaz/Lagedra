using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lagedra.TruthSurface.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "truth_surface",
                table: "snapshots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "truth_surface",
                table: "snapshots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "truth_surface",
                table: "cryptographic_proofs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "truth_surface",
                table: "cryptographic_proofs",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "truth_surface",
                table: "snapshots");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "truth_surface",
                table: "snapshots");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "truth_surface",
                table: "cryptographic_proofs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "truth_surface",
                table: "cryptographic_proofs");
        }
    }
}
