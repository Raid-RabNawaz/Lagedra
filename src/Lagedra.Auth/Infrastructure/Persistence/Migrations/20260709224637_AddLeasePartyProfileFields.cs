using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lagedra.Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLeasePartyProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BrokerDreLicense",
                schema: "auth",
                table: "AspNetUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrokerName",
                schema: "auth",
                table: "AspNetUsers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrokerScopeNotes",
                schema: "auth",
                table: "AspNetUsers",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MailingCity",
                schema: "auth",
                table: "AspNetUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MailingCountry",
                schema: "auth",
                table: "AspNetUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MailingState",
                schema: "auth",
                table: "AspNetUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MailingStreet",
                schema: "auth",
                table: "AspNetUsers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MailingZip",
                schema: "auth",
                table: "AspNetUsers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NoticeAddressSameAsMailing",
                schema: "auth",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "NoticeCity",
                schema: "auth",
                table: "AspNetUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoticeCountry",
                schema: "auth",
                table: "AspNetUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoticeState",
                schema: "auth",
                table: "AspNetUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoticeStreet",
                schema: "auth",
                table: "AspNetUsers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoticeZip",
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
                name: "BrokerDreLicense",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BrokerName",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BrokerScopeNotes",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MailingCity",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MailingCountry",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MailingState",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MailingStreet",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MailingZip",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NoticeAddressSameAsMailing",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NoticeCity",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NoticeCountry",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NoticeState",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NoticeStreet",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NoticeZip",
                schema: "auth",
                table: "AspNetUsers");
        }
    }
}
