using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaseAgreements.Migrations
{
    /// <inheritdoc />
    public partial class AddDealLeaseSignatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "deal_lease_signatures",
                schema: "lease_agreements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DealId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SignerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TypedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DocumentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deal_lease_signatures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_deal_lease_signatures_DealId_Role",
                schema: "lease_agreements",
                table: "deal_lease_signatures",
                columns: new[] { "DealId", "Role" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deal_lease_signatures",
                schema: "lease_agreements");
        }
    }
}
