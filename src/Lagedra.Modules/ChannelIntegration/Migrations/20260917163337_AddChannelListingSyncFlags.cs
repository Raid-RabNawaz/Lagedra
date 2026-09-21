using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChannelIntegration.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelListingSyncFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SyncCalendarFromPms",
                schema: "channel_integration",
                table: "channel_listing_maps",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SyncPriceFromPms",
                schema: "channel_integration",
                table: "channel_listing_maps",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SyncCalendarFromPms",
                schema: "channel_integration",
                table: "channel_listing_maps");

            migrationBuilder.DropColumn(
                name: "SyncPriceFromPms",
                schema: "channel_integration",
                table: "channel_listing_maps");
        }
    }
}
