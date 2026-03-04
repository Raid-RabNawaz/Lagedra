using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lagedra.Compliance.Migrations
{
    /// <inheritdoc />
    public partial class AddGapFixesCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TargetUserId",
                schema: "compliance",
                table: "violations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_violations_TargetUserId",
                schema: "compliance",
                table: "violations",
                column: "TargetUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_violations_TargetUserId",
                schema: "compliance",
                table: "violations");

            migrationBuilder.DropColumn(
                name: "TargetUserId",
                schema: "compliance",
                table: "violations");
        }
    }
}
