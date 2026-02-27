using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActivationAndBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "deal_payment_confirmations",
                schema: "activation_billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DealId = table.Column<Guid>(type: "uuid", nullable: false),
                    HostConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    HostConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantDisputed = table.Column<bool>(type: "boolean", nullable: false),
                    TenantDisputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DisputeReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DisputeEvidenceManifestId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    GracePeriodExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deal_payment_confirmations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_deal_payment_confirmations_DealId",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                column: "DealId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_deal_payment_confirmations_Status",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deal_payment_confirmations",
                schema: "activation_billing");
        }
    }
}
