using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityAndVerification.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "identity",
                table: "verification_cases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "identity",
                table: "verification_cases",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "identity",
                table: "identity_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "identity",
                table: "identity_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "identity",
                table: "host_payment_details",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "identity",
                table: "host_payment_details",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "identity",
                table: "fraud_flags",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "identity",
                table: "fraud_flags",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "identity",
                table: "background_check_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "identity",
                table: "background_check_reports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "identity",
                table: "affiliation_verifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "identity",
                table: "affiliation_verifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "identity",
                table: "verification_cases");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "identity",
                table: "verification_cases");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "identity",
                table: "identity_profiles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "identity",
                table: "identity_profiles");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "identity",
                table: "host_payment_details");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "identity",
                table: "host_payment_details");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "identity",
                table: "fraud_flags");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "identity",
                table: "fraud_flags");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "identity",
                table: "background_check_reports");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "identity",
                table: "background_check_reports");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "identity",
                table: "affiliation_verifications");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "identity",
                table: "affiliation_verifications");
        }
    }
}
