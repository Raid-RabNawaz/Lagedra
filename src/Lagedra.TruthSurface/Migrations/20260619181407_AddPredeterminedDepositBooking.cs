using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lagedra.TruthSurface.Migrations
{
    /// <inheritdoc />
    public partial class AddPredeterminedDepositBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "HostConsentAt",
                schema: "truth_surface",
                table: "snapshots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HostConsentIp",
                schema: "truth_surface",
                table: "snapshots",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HostConsentUserAgent",
                schema: "truth_surface",
                table: "snapshots",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HostConsentUserId",
                schema: "truth_surface",
                table: "snapshots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HostConsentVersion",
                schema: "truth_surface",
                table: "snapshots",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                schema: "truth_surface",
                table: "snapshots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAt",
                schema: "truth_surface",
                table: "snapshots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TenantConsentAt",
                schema: "truth_surface",
                table: "snapshots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantConsentIp",
                schema: "truth_surface",
                table: "snapshots",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantConsentUserAgent",
                schema: "truth_surface",
                table: "snapshots",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantConsentUserId",
                schema: "truth_surface",
                table: "snapshots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantConsentVersion",
                schema: "truth_surface",
                table: "snapshots",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HostConsentAt",
                schema: "truth_surface",
                table: "snapshots");

            migrationBuilder.DropColumn(
                name: "HostConsentIp",
                schema: "truth_surface",
                table: "snapshots");

            migrationBuilder.DropColumn(
                name: "HostConsentUserAgent",
                schema: "truth_surface",
                table: "snapshots");

            migrationBuilder.DropColumn(
                name: "HostConsentUserId",
                schema: "truth_surface",
                table: "snapshots");

            migrationBuilder.DropColumn(
                name: "HostConsentVersion",
                schema: "truth_surface",
                table: "snapshots");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                schema: "truth_surface",
                table: "snapshots");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                schema: "truth_surface",
                table: "snapshots");

            migrationBuilder.DropColumn(
                name: "TenantConsentAt",
                schema: "truth_surface",
                table: "snapshots");

            migrationBuilder.DropColumn(
                name: "TenantConsentIp",
                schema: "truth_surface",
                table: "snapshots");

            migrationBuilder.DropColumn(
                name: "TenantConsentUserAgent",
                schema: "truth_surface",
                table: "snapshots");

            migrationBuilder.DropColumn(
                name: "TenantConsentUserId",
                schema: "truth_surface",
                table: "snapshots");

            migrationBuilder.DropColumn(
                name: "TenantConsentVersion",
                schema: "truth_surface",
                table: "snapshots");
        }
    }
}
