using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lagedra.Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHostKindToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HostKind",
                schema: "auth",
                table: "AspNetUsers",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Individual");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HostKind",
                schema: "auth",
                table: "AspNetUsers");
        }
    }
}
