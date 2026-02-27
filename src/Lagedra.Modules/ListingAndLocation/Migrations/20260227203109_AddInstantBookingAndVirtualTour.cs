using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListingAndLocation.Migrations
{
    /// <inheritdoc />
    public partial class AddInstantBookingAndVirtualTour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InstantBookingEnabled",
                schema: "listings",
                table: "listings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VirtualTourUrl",
                schema: "listings",
                table: "listings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstantBookingEnabled",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "VirtualTourUrl",
                schema: "listings",
                table: "listings");
        }
    }
}
