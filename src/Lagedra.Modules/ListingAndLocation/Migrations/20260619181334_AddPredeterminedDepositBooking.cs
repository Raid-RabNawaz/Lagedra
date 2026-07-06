using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListingAndLocation.Migrations
{
    /// <inheritdoc />
    public partial class AddPredeterminedDepositBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DepositBackgroundVerifiedCents",
                schema: "listings",
                table: "listings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DepositPartnerGuaranteedCents",
                schema: "listings",
                table: "listings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DepositUnverifiedCents",
                schema: "listings",
                table: "listings",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepositBackgroundVerifiedCents",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "DepositPartnerGuaranteedCents",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "DepositUnverifiedCents",
                schema: "listings",
                table: "listings");
        }
    }
}
