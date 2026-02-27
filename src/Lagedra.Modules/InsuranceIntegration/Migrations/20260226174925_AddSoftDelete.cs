using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceIntegration.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "insurance",
                table: "verification_attempts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "insurance",
                table: "verification_attempts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "insurance",
                table: "policy_records",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "insurance",
                table: "policy_records",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "insurance",
                table: "verification_attempts");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "insurance",
                table: "verification_attempts");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "insurance",
                table: "policy_records");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "insurance",
                table: "policy_records");
        }
    }
}
