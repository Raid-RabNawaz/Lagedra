using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartnerNetwork.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreatePartnerNetwork : Migration
    {
        private static readonly string[] partner_members = new[] { "OrganizationId", "UserId" };
        private static readonly string[] referal_redemtions = new[] { "ReferralLinkId", "RedeemedByUserId" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "partner_network");

            migrationBuilder.CreateTable(
                name: "direct_reservations",
                schema: "partner_network",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuestName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    GuestEmail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    DealApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReservedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_direct_reservations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "partner_network",
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

            migrationBuilder.CreateTable(
                name: "partner_members",
                schema: "partner_network",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberRole = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InvitedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_members", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "partner_organizations",
                schema: "partner_network",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OrganizationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TaxId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    SuspensionReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "referral_links",
                schema: "partner_network",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaxUses = table.Column<int>(type: "integer", nullable: true),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_referral_links", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "referral_redemptions",
                schema: "partner_network",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferralLinkId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RedeemedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RedeemedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_referral_redemptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_direct_reservations_ListingId",
                schema: "partner_network",
                table: "direct_reservations",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_direct_reservations_OrganizationId",
                schema: "partner_network",
                table: "direct_reservations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt",
                schema: "partner_network",
                table: "outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_partner_members_OrganizationId_UserId",
                schema: "partner_network",
                table: "partner_members",
                columns: partner_members,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_partner_members_UserId",
                schema: "partner_network",
                table: "partner_members",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_partner_organizations_Name",
                schema: "partner_network",
                table: "partner_organizations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_partner_organizations_Status",
                schema: "partner_network",
                table: "partner_organizations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_referral_links_Code",
                schema: "partner_network",
                table: "referral_links",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_referral_links_OrganizationId",
                schema: "partner_network",
                table: "referral_links",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_referral_redemptions_RedeemedByUserId",
                schema: "partner_network",
                table: "referral_redemptions",
                column: "RedeemedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_referral_redemptions_ReferralLinkId_RedeemedByUserId",
                schema: "partner_network",
                table: "referral_redemptions",
                columns: referal_redemtions,
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "direct_reservations",
                schema: "partner_network");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "partner_network");

            migrationBuilder.DropTable(
                name: "partner_members",
                schema: "partner_network");

            migrationBuilder.DropTable(
                name: "partner_organizations",
                schema: "partner_network");

            migrationBuilder.DropTable(
                name: "referral_links",
                schema: "partner_network");

            migrationBuilder.DropTable(
                name: "referral_redemptions",
                schema: "partner_network");
        }
    }
}
