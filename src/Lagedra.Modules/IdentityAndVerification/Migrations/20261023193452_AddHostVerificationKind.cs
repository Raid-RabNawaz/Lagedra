using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityAndVerification.Migrations
{
    /// <inheritdoc />
    public partial class AddHostVerificationKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessJurisdiction",
                schema: "identity",
                table: "identity_profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessRegistrationNumber",
                schema: "identity",
                table: "identity_profiles",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HostKind",
                schema: "identity",
                table: "identity_profiles",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Individual");

            migrationBuilder.AddColumn<string>(
                name: "LegalBusinessName",
                schema: "identity",
                table: "identity_profiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessJurisdiction",
                schema: "identity",
                table: "identity_profiles");

            migrationBuilder.DropColumn(
                name: "BusinessRegistrationNumber",
                schema: "identity",
                table: "identity_profiles");

            migrationBuilder.DropColumn(
                name: "HostKind",
                schema: "identity",
                table: "identity_profiles");

            migrationBuilder.DropColumn(
                name: "LegalBusinessName",
                schema: "identity",
                table: "identity_profiles");
        }
    }
}
