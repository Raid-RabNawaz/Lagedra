using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lagedra.Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPreLaunchSignupFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                schema: "auth",
                table: "AspNetUsers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HousingType",
                schema: "auth",
                table: "AspNetUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPreLaunchSignup",
                schema: "auth",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PlacementsPerYear",
                schema: "auth",
                table: "AspNetUsers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortfolioSize",
                schema: "auth",
                table: "AspNetUsers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignupType",
                schema: "auth",
                table: "AspNetUsers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyName",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "HousingType",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsPreLaunchSignup",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PlacementsPerYear",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PortfolioSize",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SignupType",
                schema: "auth",
                table: "AspNetUsers");
        }
    }
}
