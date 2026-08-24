using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListingAndLocation.Migrations
{
    /// <inheritdoc />
    public partial class AddListingAddedVia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddedVia",
                schema: "listings",
                table: "listings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<string>(
                name: "AddedViaDetail",
                schema: "listings",
                table: "listings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddedVia",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "AddedViaDetail",
                schema: "listings",
                table: "listings");
        }
    }
}
