using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartnerNetwork.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerOrganizationStripeCustomerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                schema: "partner_network",
                table: "partner_organizations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                schema: "partner_network",
                table: "partner_organizations");
        }
    }
}
