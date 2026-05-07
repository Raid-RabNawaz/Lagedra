using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartnerNetwork.Migrations
{
    /// <inheritdoc />
    public partial class AddEndorsementsAndGuestInvitesAndConsent : Migration
    {
        private static readonly string[] OrganizationId_TenantId = new[] { "OrganizationId", "TenantUserId" };
        private static readonly string[] OrganizationId_Status = new[] { "OrganizationId", "Status" };
        private static readonly string[] TenantUserId_Status = new[] { "TenantUserId", "Status" };
        private static readonly string[] OrganizationId_InvitedAt = new[] { "OrganizationId", "InvitedAt" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndorsementTermsAcceptedAt",
                schema: "partner_network",
                table: "partner_organizations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "EndorsementTermsAcceptedByUserId",
                schema: "partner_network",
                table: "partner_organizations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "partner_endorsements",
                schema: "partner_network",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevokeReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_endorsements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "partner_guest_invites",
                schema: "partner_network",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FullName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    WasUserJustCreated = table.Column<bool>(type: "boolean", nullable: false),
                    EndorsementId = table.Column<Guid>(type: "uuid", nullable: true),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_guest_invites", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_partner_endorsements_active_per_tenant_per_org",
                schema: "partner_network",
                table: "partner_endorsements",
                columns: OrganizationId_TenantId,
                unique: true,
                filter: "\"Status\" IN ('Requested', 'Approved')");

            migrationBuilder.CreateIndex(
                name: "IX_partner_endorsements_ExpiresAt",
                schema: "partner_network",
                table: "partner_endorsements",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_partner_endorsements_OrganizationId_Status",
                schema: "partner_network",
                table: "partner_endorsements",
                columns: OrganizationId_Status);

            migrationBuilder.CreateIndex(
                name: "IX_partner_endorsements_TenantUserId_Status",
                schema: "partner_network",
                table: "partner_endorsements",
                columns: TenantUserId_Status);

            migrationBuilder.CreateIndex(
                name: "IX_partner_guest_invites_InvitedUserId",
                schema: "partner_network",
                table: "partner_guest_invites",
                column: "InvitedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_partner_guest_invites_OrganizationId",
                schema: "partner_network",
                table: "partner_guest_invites",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_partner_guest_invites_OrganizationId_InvitedAt",
                schema: "partner_network",
                table: "partner_guest_invites",
                columns: OrganizationId_InvitedAt);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "partner_endorsements",
                schema: "partner_network");

            migrationBuilder.DropTable(
                name: "partner_guest_invites",
                schema: "partner_network");

            migrationBuilder.DropColumn(
                name: "EndorsementTermsAcceptedAt",
                schema: "partner_network",
                table: "partner_organizations");

            migrationBuilder.DropColumn(
                name: "EndorsementTermsAcceptedByUserId",
                schema: "partner_network",
                table: "partner_organizations");
        }
    }
}
