using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arbitration.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateArbitration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "arbitration");

            migrationBuilder.CreateTable(
                name: "arbitration_cases",
                schema: "arbitration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DealId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tier = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FiledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EvidenceCompleteAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecisionDueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecisionSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    AwardAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_arbitration_cases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "arbitration",
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
                name: "arbitrator_assignments",
                schema: "arbitration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArbitratorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConcurrentCaseCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_arbitrator_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_arbitrator_assignments_arbitration_cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "arbitration",
                        principalTable: "arbitration_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evidence_slots",
                schema: "arbitration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SubmittedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    FileReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_slots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evidence_slots_arbitration_cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "arbitration",
                        principalTable: "arbitration_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_arbitration_cases_DealId",
                schema: "arbitration",
                table: "arbitration_cases",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_arbitration_cases_Status",
                schema: "arbitration",
                table: "arbitration_cases",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_arbitrator_assignments_ArbitratorUserId",
                schema: "arbitration",
                table: "arbitrator_assignments",
                column: "ArbitratorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_arbitrator_assignments_CaseId",
                schema: "arbitration",
                table: "arbitrator_assignments",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_evidence_slots_CaseId",
                schema: "arbitration",
                table: "evidence_slots",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt",
                schema: "arbitration",
                table: "outbox_messages",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "arbitrator_assignments",
                schema: "arbitration");

            migrationBuilder.DropTable(
                name: "evidence_slots",
                schema: "arbitration");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "arbitration");

            migrationBuilder.DropTable(
                name: "arbitration_cases",
                schema: "arbitration");
        }
    }
}
