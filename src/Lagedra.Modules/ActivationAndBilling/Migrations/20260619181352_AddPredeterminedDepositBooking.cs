using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActivationAndBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddPredeterminedDepositBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DepositReason",
                schema: "activation_billing",
                table: "deal_applications",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HostConsentIpAddress",
                schema: "activation_billing",
                table: "deal_applications",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HostConsentUserAgent",
                schema: "activation_billing",
                table: "deal_applications",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HostConsentVersion",
                schema: "activation_billing",
                table: "deal_applications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HostTruthSurfaceConsentAt",
                schema: "activation_billing",
                table: "deal_applications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HostTruthSurfaceConsentGiven",
                schema: "activation_billing",
                table: "deal_applications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "ServiceFeeCents",
                schema: "activation_billing",
                table: "deal_applications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantConsentIpAddress",
                schema: "activation_billing",
                table: "deal_applications",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantConsentUserAgent",
                schema: "activation_billing",
                table: "deal_applications",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantConsentVersion",
                schema: "activation_billing",
                table: "deal_applications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TenantTruthSurfaceConsentAt",
                schema: "activation_billing",
                table: "deal_applications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TenantTruthSurfaceConsentGiven",
                schema: "activation_billing",
                table: "deal_applications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TenantVerificationTierAtRequest",
                schema: "activation_billing",
                table: "deal_applications",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TotalPayableSnapshotCents",
                schema: "activation_billing",
                table: "deal_applications",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepositReason",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "HostConsentIpAddress",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "HostConsentUserAgent",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "HostConsentVersion",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "HostTruthSurfaceConsentAt",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "HostTruthSurfaceConsentGiven",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "ServiceFeeCents",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "TenantConsentIpAddress",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "TenantConsentUserAgent",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "TenantConsentVersion",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "TenantTruthSurfaceConsentAt",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "TenantTruthSurfaceConsentGiven",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "TenantVerificationTierAtRequest",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "TotalPayableSnapshotCents",
                schema: "activation_billing",
                table: "deal_applications");
        }
    }
}
