using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arbitration.Migrations
{
    /// <inheritdoc />
    public partial class AddArbitrationFilingFeePayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FilingFeePaidAt",
                schema: "arbitration",
                table: "arbitration_cases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilingFeePaymentIntentId",
                schema: "arbitration",
                table: "arbitration_cases",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilingFeePaidAt",
                schema: "arbitration",
                table: "arbitration_cases");

            migrationBuilder.DropColumn(
                name: "FilingFeePaymentIntentId",
                schema: "arbitration",
                table: "arbitration_cases");
        }
    }
}
