using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListingAndLocation.Migrations
{
    /// <inheritdoc />
    public partial class AddListingManagementAndBrokerClause : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "HomeOwnerUserId",
                schema: "listings",
                table: "listings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeBrokerClause",
                schema: "listings",
                table: "listings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ManagerRole",
                schema: "listings",
                table: "listings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Owner");

            migrationBuilder.CreateIndex(
                name: "IX_listings_HomeOwnerUserId",
                schema: "listings",
                table: "listings",
                column: "HomeOwnerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_listings_HomeOwnerUserId",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "HomeOwnerUserId",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "IncludeBrokerClause",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "ManagerRole",
                schema: "listings",
                table: "listings");
        }
    }
}
