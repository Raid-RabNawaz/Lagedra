using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActivationAndBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddDealApplicationSourceColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Source",
                schema: "activation_billing",
                table: "deal_applications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_deal_applications_Source",
                schema: "activation_billing",
                table: "deal_applications",
                column: "Source");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_deal_applications_Source",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "Source",
                schema: "activation_billing",
                table: "deal_applications");
        }
    }
}
