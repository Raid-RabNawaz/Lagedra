using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AntiAbuseAndIntegrity.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateAntiAbuseAndIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "integrity");

            migrationBuilder.CreateTable(
                name: "abuse_cases",
                schema: "integrity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AbuseType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abuse_cases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "account_restrictions",
                schema: "integrity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RestrictionLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_restrictions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "collusion_patterns",
                schema: "integrity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AbuseCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyAUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyBUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RepeatedDealCount = table.Column<int>(type: "integer", nullable: false),
                    FirstOccurrence = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LatestOccurrence = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_collusion_patterns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fraud_flags",
                schema: "integrity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FlagType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FlaggedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fraud_flags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "integrity",
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

            migrationBuilder.CreateIndex(
                name: "IX_abuse_cases_SubjectUserId",
                schema: "integrity",
                table: "abuse_cases",
                column: "SubjectUserId");

            migrationBuilder.CreateIndex(
                name: "IX_account_restrictions_UserId",
                schema: "integrity",
                table: "account_restrictions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_collusion_patterns_AbuseCaseId",
                schema: "integrity",
                table: "collusion_patterns",
                column: "AbuseCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_fraud_flags_UserId",
                schema: "integrity",
                table: "fraud_flags",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt",
                schema: "integrity",
                table: "outbox_messages",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "abuse_cases",
                schema: "integrity");

            migrationBuilder.DropTable(
                name: "account_restrictions",
                schema: "integrity");

            migrationBuilder.DropTable(
                name: "collusion_patterns",
                schema: "integrity");

            migrationBuilder.DropTable(
                name: "fraud_flags",
                schema: "integrity");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "integrity");
        }
    }
}
