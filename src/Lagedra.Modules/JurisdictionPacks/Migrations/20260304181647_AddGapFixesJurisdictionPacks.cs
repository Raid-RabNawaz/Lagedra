using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // EF Core migration scaffolded code

namespace Lagedra.Modules.JurisdictionPacks.Migrations
{
    /// <inheritdoc />
    public partial class AddGapFixesJurisdictionPacks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "deposit_cap_rules",
                schema: "jurisdiction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    JurisdictionCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaxMultiplier = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ExceptionCondition = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExceptionMultiplier = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    LegalReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deposit_cap_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_deposit_cap_rules_pack_versions_VersionId",
                        column: x => x.VersionId,
                        principalSchema: "jurisdiction",
                        principalTable: "pack_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_deposit_cap_rules_VersionId_JurisdictionCode",
                schema: "jurisdiction",
                table: "deposit_cap_rules",
                columns: new[] { "VersionId", "JurisdictionCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deposit_cap_rules",
                schema: "jurisdiction");
        }
    }
}
