using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lagedra.TruthSurface.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "truth_surface");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "truth_surface",
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
                name: "snapshots",
                schema: "truth_surface",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DealId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SealedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CanonicalContent = table.Column<string>(type: "text", nullable: true),
                    Hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Signature = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ProtocolVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    JurisdictionPackVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    InquiryClosed = table.Column<bool>(type: "boolean", nullable: false),
                    LandlordConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TenantConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    SupersededBySnapshotId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cryptographic_proofs",
                schema: "truth_surface",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Signature = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cryptographic_proofs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cryptographic_proofs_snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalSchema: "truth_surface",
                        principalTable: "snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cryptographic_proofs_SnapshotId",
                schema: "truth_surface",
                table: "cryptographic_proofs",
                column: "SnapshotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt",
                schema: "truth_surface",
                table: "outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_snapshots_DealId",
                schema: "truth_surface",
                table: "snapshots",
                column: "DealId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cryptographic_proofs",
                schema: "truth_surface");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "truth_surface");

            migrationBuilder.DropTable(
                name: "snapshots",
                schema: "truth_surface");
        }
    }
}
