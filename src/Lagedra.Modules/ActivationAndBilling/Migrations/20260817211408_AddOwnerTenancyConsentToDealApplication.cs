using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActivationAndBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerTenancyConsentToDealApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "HomeOwnerUserId",
                schema: "activation_billing",
                table: "deal_applications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerConsentIpAddress",
                schema: "activation_billing",
                table: "deal_applications",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OwnerConsentRequired",
                schema: "activation_billing",
                table: "deal_applications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OwnerConsentUserAgent",
                schema: "activation_billing",
                table: "deal_applications",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerConsentVersion",
                schema: "activation_billing",
                table: "deal_applications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OwnerTenancyConsentAt",
                schema: "activation_billing",
                table: "deal_applications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OwnerTenancyConsentDeclined",
                schema: "activation_billing",
                table: "deal_applications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OwnerTenancyConsentDeclinedAt",
                schema: "activation_billing",
                table: "deal_applications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OwnerTenancyConsentGiven",
                schema: "activation_billing",
                table: "deal_applications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_deal_applications_HomeOwnerUserId",
                schema: "activation_billing",
                table: "deal_applications",
                column: "HomeOwnerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_deal_applications_HomeOwnerUserId",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "HomeOwnerUserId",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "OwnerConsentIpAddress",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "OwnerConsentRequired",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "OwnerConsentUserAgent",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "OwnerConsentVersion",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "OwnerTenancyConsentAt",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "OwnerTenancyConsentDeclined",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "OwnerTenancyConsentDeclinedAt",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "OwnerTenancyConsentGiven",
                schema: "activation_billing",
                table: "deal_applications");
        }
    }
}
