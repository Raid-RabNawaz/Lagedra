using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StructuredInquiry.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "inquiry",
                table: "sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "inquiry",
                table: "sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "inquiry",
                table: "questions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "inquiry",
                table: "questions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "inquiry",
                table: "answers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "inquiry",
                table: "answers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "inquiry",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "inquiry",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "inquiry",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "inquiry",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "inquiry",
                table: "answers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "inquiry",
                table: "answers");
        }
    }
}
