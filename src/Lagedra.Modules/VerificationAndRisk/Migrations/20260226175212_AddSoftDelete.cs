using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VerificationAndRisk.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "risk",
                table: "risk_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "risk",
                table: "risk_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "risk",
                table: "risk_profiles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "risk",
                table: "risk_profiles");
        }
    }
}
