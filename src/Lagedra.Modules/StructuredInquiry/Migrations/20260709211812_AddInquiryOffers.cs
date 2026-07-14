using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StructuredInquiry.Migrations
{
    /// <inheritdoc />
    public partial class AddInquiryOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "offers",
                schema: "inquiry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProposedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProposedByRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RentCents = table.Column<long>(type: "bigint", nullable: false),
                    DepositCents = table.Column<long>(type: "bigint", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProposedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SupersedesOfferId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_offers_sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "inquiry",
                        principalTable: "sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_offers_SessionId",
                schema: "inquiry",
                table: "offers",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_offers_SessionId_Status",
                schema: "inquiry",
                table: "offers",
                columns: new[] { "SessionId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "offers",
                schema: "inquiry");
        }
    }
}
