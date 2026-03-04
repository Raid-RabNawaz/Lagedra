using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListingAndLocation.Migrations
{
    /// <inheritdoc />
    public partial class AddDepositFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "MaxDepositCents",
                schema: "listings",
                table: "listings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "SuggestedDepositHighCents",
                schema: "listings",
                table: "listings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SuggestedDepositLowCents",
                schema: "listings",
                table: "listings",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxDepositCents",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "SuggestedDepositHighCents",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "SuggestedDepositLowCents",
                schema: "listings",
                table: "listings");
        }
    }
}
