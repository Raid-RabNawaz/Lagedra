using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lagedra.Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneVerificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneVerificationCodeHash",
                schema: "auth",
                table: "AspNetUsers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PhoneVerificationExpiresAt",
                schema: "auth",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PhoneVerificationSendCount",
                schema: "auth",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PhoneVerificationSentAt",
                schema: "auth",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PhoneVerificationWindowStartedAt",
                schema: "auth",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneVerificationCodeHash",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PhoneVerificationExpiresAt",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PhoneVerificationSendCount",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PhoneVerificationSentAt",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PhoneVerificationWindowStartedAt",
                schema: "auth",
                table: "AspNetUsers");
        }
    }
}
