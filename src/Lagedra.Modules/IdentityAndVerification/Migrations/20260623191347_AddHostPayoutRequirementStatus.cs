using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityAndVerification.Migrations
{
    /// <inheritdoc />
    public partial class AddHostPayoutRequirementStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankAccountStatus",
                schema: "identity",
                table: "host_stripe_accounts",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "TaxStatus",
                schema: "identity",
                table: "host_stripe_accounts",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Unknown");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountStatus",
                schema: "identity",
                table: "host_stripe_accounts");

            migrationBuilder.DropColumn(
                name: "TaxStatus",
                schema: "identity",
                table: "host_stripe_accounts");
        }
    }
}
