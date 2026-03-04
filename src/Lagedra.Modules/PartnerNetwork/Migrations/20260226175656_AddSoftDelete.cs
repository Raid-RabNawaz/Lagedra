using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartnerNetwork.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "partner_network",
                table: "referral_redemptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "partner_network",
                table: "referral_redemptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "partner_network",
                table: "referral_links",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "partner_network",
                table: "referral_links",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "partner_network",
                table: "partner_organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "partner_network",
                table: "partner_organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "partner_network",
                table: "partner_members",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "partner_network",
                table: "partner_members",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "partner_network",
                table: "direct_reservations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "partner_network",
                table: "direct_reservations",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "partner_network",
                table: "referral_redemptions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "partner_network",
                table: "referral_redemptions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "partner_network",
                table: "referral_links");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "partner_network",
                table: "referral_links");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "partner_network",
                table: "partner_organizations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "partner_network",
                table: "partner_organizations");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "partner_network",
                table: "partner_members");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "partner_network",
                table: "partner_members");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "partner_network",
                table: "direct_reservations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "partner_network",
                table: "direct_reservations");
        }
    }
}
