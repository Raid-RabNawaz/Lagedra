using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityAndVerification.Migrations
{
    /// <inheritdoc />
    public partial class AddScreeningOrderFailureReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastFailureReason",
                schema: "identity",
                table: "screening_orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastFailureReason",
                schema: "identity",
                table: "screening_orders");
        }
    }
}
