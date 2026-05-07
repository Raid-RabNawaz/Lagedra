using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActivationAndBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddTruthSurfaceSnapshotIdLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TruthSurfaceSnapshotId",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TruthSurfaceSnapshotId",
                schema: "activation_billing",
                table: "deal_applications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_deal_payment_confirmations_TruthSurfaceSnapshotId",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                column: "TruthSurfaceSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_deal_applications_TruthSurfaceSnapshotId",
                schema: "activation_billing",
                table: "deal_applications",
                column: "TruthSurfaceSnapshotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_deal_payment_confirmations_TruthSurfaceSnapshotId",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropIndex(
                name: "IX_deal_applications_TruthSurfaceSnapshotId",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "TruthSurfaceSnapshotId",
                schema: "activation_billing",
                table: "deal_payment_confirmations");

            migrationBuilder.DropColumn(
                name: "TruthSurfaceSnapshotId",
                schema: "activation_billing",
                table: "deal_applications");
        }
    }
}
