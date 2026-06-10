using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arbitration.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredVerdict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DecisionOutcome",
                schema: "arbitration",
                table: "arbitration_cases",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionSeverity",
                schema: "arbitration",
                table: "arbitration_cases",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsStructuredVerdict",
                schema: "arbitration",
                table: "arbitration_cases",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "decision_penalties",
                schema: "arbitration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PenaltyType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AmountCents = table.Column<long>(type: "bigint", nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_decision_penalties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_decision_penalties_arbitration_cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "arbitration",
                        principalTable: "arbitration_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_decision_penalties_CaseId",
                schema: "arbitration",
                table: "decision_penalties",
                column: "CaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "decision_penalties",
                schema: "arbitration");

            migrationBuilder.DropColumn(
                name: "DecisionOutcome",
                schema: "arbitration",
                table: "arbitration_cases");

            migrationBuilder.DropColumn(
                name: "DecisionSeverity",
                schema: "arbitration",
                table: "arbitration_cases");

            migrationBuilder.DropColumn(
                name: "IsStructuredVerdict",
                schema: "arbitration",
                table: "arbitration_cases");
        }
    }
}
