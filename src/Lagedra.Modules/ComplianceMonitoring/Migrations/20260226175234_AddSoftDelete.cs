using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceMonitoring.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "compliance_monitoring",
                table: "monitored_violations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "compliance_monitoring",
                table: "monitored_violations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "compliance_monitoring",
                table: "monitored_compliance_signals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "compliance_monitoring",
                table: "monitored_compliance_signals",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "compliance_monitoring",
                table: "monitored_violations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "compliance_monitoring",
                table: "monitored_violations");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "compliance_monitoring",
                table: "monitored_compliance_signals");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "compliance_monitoring",
                table: "monitored_compliance_signals");
        }
    }
}
