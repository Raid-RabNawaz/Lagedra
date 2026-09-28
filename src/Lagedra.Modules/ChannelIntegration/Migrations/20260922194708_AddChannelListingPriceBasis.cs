using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChannelIntegration.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelListingPriceBasis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PmsPriceBasis",
                schema: "channel_integration",
                table: "channel_listing_maps",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PmsSourceAmountCents",
                schema: "channel_integration",
                table: "channel_listing_maps",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PmsPriceBasis",
                schema: "channel_integration",
                table: "channel_listing_maps");

            migrationBuilder.DropColumn(
                name: "PmsSourceAmountCents",
                schema: "channel_integration",
                table: "channel_listing_maps");
        }
    }
}
