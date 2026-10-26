using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lagedra.Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHostingEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HostingEnabled",
                schema: "auth",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Accounts that joined through the old host door already host.
            // New Member signups stay false until they choose Become a host.
            migrationBuilder.Sql(
                """
                UPDATE auth."AspNetUsers"
                SET "HostingEnabled" = TRUE
                WHERE "SignupType" = 'Host';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HostingEnabled",
                schema: "auth",
                table: "AspNetUsers");
        }
    }
}
