using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arbitration.Migrations
{
    /// <inheritdoc />
    public partial class AddGapFixesArbitration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileReference",
                schema: "arbitration",
                table: "evidence_slots");

            migrationBuilder.AddColumn<Guid>(
                name: "EvidenceManifestId",
                schema: "arbitration",
                table: "evidence_slots",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "FiledByUserId",
                schema: "arbitration",
                table: "arbitration_cases",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<long>(
                name: "FilingFeeCents",
                schema: "arbitration",
                table: "arbitration_cases",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_evidence_slots_EvidenceManifestId",
                schema: "arbitration",
                table: "evidence_slots",
                column: "EvidenceManifestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_evidence_slots_EvidenceManifestId",
                schema: "arbitration",
                table: "evidence_slots");

            migrationBuilder.DropColumn(
                name: "EvidenceManifestId",
                schema: "arbitration",
                table: "evidence_slots");

            migrationBuilder.DropColumn(
                name: "FiledByUserId",
                schema: "arbitration",
                table: "arbitration_cases");

            migrationBuilder.DropColumn(
                name: "FilingFeeCents",
                schema: "arbitration",
                table: "arbitration_cases");

            migrationBuilder.AddColumn<string>(
                name: "FileReference",
                schema: "arbitration",
                table: "evidence_slots",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
