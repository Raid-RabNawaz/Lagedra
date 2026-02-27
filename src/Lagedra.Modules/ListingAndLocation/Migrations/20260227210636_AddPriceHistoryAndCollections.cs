using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListingAndLocation.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceHistoryAndCollections : Migration
    {
        private static readonly string[] listing_history_index = new[] { "ListingId", "EffectiveFrom" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CollectionId",
                schema: "listings",
                table: "saved_listings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "listing_price_history",
                schema: "listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    MonthlyRentCents = table.Column<long>(type: "bigint", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listing_price_history", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "saved_listing_collections",
                schema: "listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_listing_collections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_saved_listings_CollectionId",
                schema: "listings",
                table: "saved_listings",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_listing_price_history_ListingId",
                schema: "listings",
                table: "listing_price_history",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_listing_price_history_ListingId_EffectiveFrom",
                schema: "listings",
                table: "listing_price_history",
                columns: listing_history_index);

            migrationBuilder.CreateIndex(
                name: "IX_saved_listing_collections_UserId",
                schema: "listings",
                table: "saved_listing_collections",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "listing_price_history",
                schema: "listings");

            migrationBuilder.DropTable(
                name: "saved_listing_collections",
                schema: "listings");

            migrationBuilder.DropIndex(
                name: "IX_saved_listings_CollectionId",
                schema: "listings",
                table: "saved_listings");

            migrationBuilder.DropColumn(
                name: "CollectionId",
                schema: "listings",
                table: "saved_listings");
        }
    }
}
