using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChannelIntegration.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateChannelIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "channel_integration");

            migrationBuilder.CreateTable(
                name: "channel_booking_links",
                schema: "channel_integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DealId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderBookingId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SyncStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PushedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_channel_booking_links", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "channel_connections",
                schema: "channel_integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HostUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExternalAccountId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Username = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EncryptedSecret = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastContentSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastBookingSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_channel_connections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "channel_listing_maps",
                schema: "channel_integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderListingId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LastImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_channel_listing_maps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "channel_sync_cursors",
                schema: "channel_integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CursorKind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastChangedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_channel_sync_cursors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "channel_integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_channel_booking_links_ConnectionId_ProviderBookingId",
                schema: "channel_integration",
                table: "channel_booking_links",
                columns: new[] { "ConnectionId", "ProviderBookingId" });

            migrationBuilder.CreateIndex(
                name: "IX_channel_booking_links_DealId",
                schema: "channel_integration",
                table: "channel_booking_links",
                column: "DealId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_channel_connections_HostUserId",
                schema: "channel_integration",
                table: "channel_connections",
                column: "HostUserId");

            migrationBuilder.CreateIndex(
                name: "IX_channel_connections_HostUserId_ProviderKey_ExternalAccountId",
                schema: "channel_integration",
                table: "channel_connections",
                columns: new[] { "HostUserId", "ProviderKey", "ExternalAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_channel_listing_maps_ConnectionId",
                schema: "channel_integration",
                table: "channel_listing_maps",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_channel_listing_maps_ConnectionId_ProviderListingId",
                schema: "channel_integration",
                table: "channel_listing_maps",
                columns: new[] { "ConnectionId", "ProviderListingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_channel_listing_maps_ListingId",
                schema: "channel_integration",
                table: "channel_listing_maps",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_channel_sync_cursors_ConnectionId_CursorKind",
                schema: "channel_integration",
                table: "channel_sync_cursors",
                columns: new[] { "ConnectionId", "CursorKind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt",
                schema: "channel_integration",
                table: "outbox_messages",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "channel_booking_links",
                schema: "channel_integration");

            migrationBuilder.DropTable(
                name: "channel_connections",
                schema: "channel_integration");

            migrationBuilder.DropTable(
                name: "channel_listing_maps",
                schema: "channel_integration");

            migrationBuilder.DropTable(
                name: "channel_sync_cursors",
                schema: "channel_integration");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "channel_integration");
        }
    }
}
