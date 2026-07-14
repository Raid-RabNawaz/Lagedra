using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StructuredInquiry.Migrations
{
    /// <inheritdoc />
    public partial class AddInquiryPartnerParticipant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PartnerAddedAt",
                schema: "inquiry",
                table: "sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PartnerAddedByUserId",
                schema: "inquiry",
                table: "sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PartnerOrganizationId",
                schema: "inquiry",
                table: "sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedByRole",
                schema: "inquiry",
                table: "questions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedByUserId",
                schema: "inquiry",
                table: "questions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_sessions_PartnerOrganizationId",
                schema: "inquiry",
                table: "sessions",
                column: "PartnerOrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sessions_PartnerOrganizationId",
                schema: "inquiry",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "PartnerAddedAt",
                schema: "inquiry",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "PartnerAddedByUserId",
                schema: "inquiry",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "PartnerOrganizationId",
                schema: "inquiry",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "SubmittedByRole",
                schema: "inquiry",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                schema: "inquiry",
                table: "questions");
        }
    }
}
