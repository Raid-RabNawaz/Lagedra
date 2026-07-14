using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActivationAndBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddDepositReturnEvidenceManifest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepositReturnEvidenceManifestId",
                schema: "activation_billing",
                table: "deal_payment_confirmations",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepositReturnEvidenceManifestId",
                schema: "activation_billing",
                table: "deal_payment_confirmations");
        }
    }
}
