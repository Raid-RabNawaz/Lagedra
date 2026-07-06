using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActivationAndBilling.Migrations
{
    /// <inheritdoc />
    public partial class SyncBillingModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DepositReturnAmountCents",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DepositReturnMethod",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DepositReturnNote",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DepositReturnReminderSentAt",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DepositReturnSettledAt",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HostConfirmedDepositReturnedAt",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MoveOutInitiatedAt",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MoveOutInitiatedByUserId",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TenantConfirmedDepositReceivedAt",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepositReturnAmountCents",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropColumn(
                name: "DepositReturnMethod",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropColumn(
                name: "DepositReturnNote",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropColumn(
                name: "DepositReturnReminderSentAt",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropColumn(
                name: "DepositReturnSettledAt",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropColumn(
                name: "HostConfirmedDepositReturnedAt",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropColumn(
                name: "MoveOutInitiatedAt",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropColumn(
                name: "MoveOutInitiatedByUserId",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropColumn(
                name: "TenantConfirmedDepositReceivedAt",
                schema: "activation_billing",
                table: "deal_payment_confirmations");
        }
    }
}
