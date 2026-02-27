using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListingAndLocation.Migrations
{
    /// <inheritdoc />
    public partial class AddListingAvailabilityBlocks : Migration
    {
        private static readonly string[] listing_availability = new[] { "ListingId", "CheckInDate", "CheckOutDate" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "listing_availability_blocks",
                schema: "listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    DealId = table.Column<Guid>(type: "uuid", nullable: true),
                    CheckInDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckOutDate = table.Column<DateOnly>(type: "date", nullable: false),
                    BlockType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listing_availability_blocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_listing_availability_blocks_listings_ListingId",
                        column: x => x.ListingId,
                        principalSchema: "listings",
                        principalTable: "listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_listing_availability_blocks_DealId",
                schema: "listings",
                table: "listing_availability_blocks",
                column: "DealId",
                filter: "\"DealId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_listing_availability_blocks_ListingId_CheckInDate_CheckOutD~",
                schema: "listings",
                table: "listing_availability_blocks",
                columns: listing_availability);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "listing_availability_blocks",
                schema: "listings");
        }
    }
}
