using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActivationAndBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddDealFinancialsAndPlatformPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HostPaidPlatform",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "HostPaidPlatformAt",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TotalHostPlatformPaymentCents",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "TotalTenantPaymentCents",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "DepositAmountCents",
                schema: "activation_billing",
                table: "deal_applications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FirstMonthRentCents",
                schema: "activation_billing",
                table: "deal_applications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "InsuranceFeeCents",
                schema: "activation_billing",
                table: "deal_applications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPartnerReferred",
                schema: "activation_billing",
                table: "deal_applications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "JurisdictionWarning",
                schema: "activation_billing",
                table: "deal_applications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PartnerOrganizationId",
                schema: "activation_billing",
                table: "deal_applications",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HostPaidPlatform",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropColumn(
                name: "HostPaidPlatformAt",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropColumn(
                name: "TotalHostPlatformPaymentCents",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropColumn(
                name: "TotalTenantPaymentCents",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropColumn(
                name: "DepositAmountCents",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "FirstMonthRentCents",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "InsuranceFeeCents",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "IsPartnerReferred",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "JurisdictionWarning",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "PartnerOrganizationId",
                schema: "activation_billing",
                table: "deal_applications");
        }
    }
}
