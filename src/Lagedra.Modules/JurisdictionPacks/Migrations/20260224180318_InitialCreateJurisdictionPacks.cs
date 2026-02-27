using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lagedra.Modules.JurisdictionPacks.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateJurisdictionPacks : Migration
    {
        private static readonly string[] _effectiveDateRuleIndexColumns = ["VersionId", "FieldName"];
        private static readonly string[] _packVersionIndexColumns = ["PackId", "VersionNumber"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "jurisdiction");

            migrationBuilder.CreateTable(
                name: "jurisdiction_packs",
                schema: "jurisdiction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    jurisdiction_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ActiveVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jurisdiction_packs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "jurisdiction",
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
                name: "pack_versions",
                schema: "jurisdiction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    SecondApproverId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pack_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pack_versions_jurisdiction_packs_PackId",
                        column: x => x.PackId,
                        principalSchema: "jurisdiction",
                        principalTable: "jurisdiction_packs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "effective_date_rules",
                schema: "jurisdiction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_effective_date_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_effective_date_rules_pack_versions_VersionId",
                        column: x => x.VersionId,
                        principalSchema: "jurisdiction",
                        principalTable: "pack_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evidence_schedules",
                schema: "jurisdiction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MinimumRequirements = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evidence_schedules_pack_versions_VersionId",
                        column: x => x.VersionId,
                        principalSchema: "jurisdiction",
                        principalTable: "pack_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "field_gating_rules",
                schema: "jurisdiction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GatingType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Condition = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_field_gating_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_field_gating_rules_pack_versions_VersionId",
                        column: x => x.VersionId,
                        principalSchema: "jurisdiction",
                        principalTable: "pack_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_effective_date_rules_VersionId_FieldName",
                schema: "jurisdiction",
                table: "effective_date_rules",
                columns: _effectiveDateRuleIndexColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_evidence_schedules_VersionId",
                schema: "jurisdiction",
                table: "evidence_schedules",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_field_gating_rules_VersionId",
                schema: "jurisdiction",
                table: "field_gating_rules",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_jurisdiction_packs_jurisdiction_code",
                schema: "jurisdiction",
                table: "jurisdiction_packs",
                column: "jurisdiction_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt",
                schema: "jurisdiction",
                table: "outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_pack_versions_PackId_VersionNumber",
                schema: "jurisdiction",
                table: "pack_versions",
                columns: _packVersionIndexColumns,
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "effective_date_rules",
                schema: "jurisdiction");

            migrationBuilder.DropTable(
                name: "evidence_schedules",
                schema: "jurisdiction");

            migrationBuilder.DropTable(
                name: "field_gating_rules",
                schema: "jurisdiction");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "jurisdiction");

            migrationBuilder.DropTable(
                name: "pack_versions",
                schema: "jurisdiction");

            migrationBuilder.DropTable(
                name: "jurisdiction_packs",
                schema: "jurisdiction");
        }
    }
}
