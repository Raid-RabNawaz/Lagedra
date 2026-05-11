using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActivationAndBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddStripePaymentTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DepositAmountCents",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "FirstMonthRentCents",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "InsuranceFeeCents",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "MonthlyProtocolFeeCents",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "StripePaymentIntentId",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripePaymentStatus",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_deal_payment_confirmations_StripePaymentIntentId",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                column: "StripePaymentIntentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_deal_payment_confirmations_StripePaymentIntentId",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropColumn(
                name: "DepositAmountCents",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropColumn(
                name: "FirstMonthRentCents",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropColumn(
                name: "InsuranceFeeCents",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropColumn(
                name: "MonthlyProtocolFeeCents",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropColumn(
                name: "StripePaymentIntentId",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropColumn(
                name: "StripePaymentStatus",
                schema: "activation_billing",
                table: "deal_payment_confirmations");
        }
    }
}
