using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActivationAndBilling.Migrations
{
    /// <inheritdoc />
    public partial class CheckPendingBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HostPlatformReminderSentAt",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderSentAt",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "damage_claims",
                schema: "activation_billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DealId = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    FiledByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ClaimedAmountCents = table.Column<long>(type: "bigint", nullable: false),
                    ApprovedAmountCents = table.Column<long>(type: "bigint", nullable: true),
                    DepositDeductionCents = table.Column<long>(type: "bigint", nullable: false),
                    InsuranceClaimCents = table.Column<long>(type: "bigint", nullable: true),
                    EvidenceManifestId = table.Column<Guid>(type: "uuid", nullable: true),
                    FiledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_damage_claims", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_damage_claims_DealId",
                schema: "activation_billing",
                table: "damage_claims",
                column: "DealId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_damage_claims_FiledByUserId",
                schema: "activation_billing",
                table: "damage_claims",
                column: "FiledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_damage_claims_TenantUserId",
                schema: "activation_billing",
                table: "damage_claims",
                column: "TenantUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "damage_claims",
                schema: "activation_billing");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropColumn(
                name: "HostPlatformReminderSentAt",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropColumn(
                name: "ReminderSentAt",
                schema: "activation_billing",
                table: "deal_payment_confirmations");
        }
    }
}
