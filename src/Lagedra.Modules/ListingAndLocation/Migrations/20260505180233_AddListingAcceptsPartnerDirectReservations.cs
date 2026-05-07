using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListingAndLocation.Migrations
{
    /// <inheritdoc />
    public partial class AddListingAcceptsPartnerDirectReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcceptsPartnerDirectReservations",
                schema: "listings",
                table: "listings",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptsPartnerDirectReservations",
                schema: "listings",
                table: "listings");
        }
    }
}
