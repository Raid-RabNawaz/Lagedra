using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceIntegration.Migrations
{
    /// <inheritdoc />
    public partial class AddTruviScreeningToPolicyRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalVerificationId",
                schema: "insurance",
                table: "policy_records",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlaggedReason",
                schema: "insurance",
                table: "policy_records",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScreeningStatus",
                schema: "insurance",
                table: "policy_records",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalVerificationId",
                schema: "insurance",
                table: "policy_records");

            migrationBuilder.DropColumn(
                name: "FlaggedReason",
                schema: "insurance",
                table: "policy_records");

            migrationBuilder.DropColumn(
                name: "ScreeningStatus",
                schema: "insurance",
                table: "policy_records");
        }
    }
}
