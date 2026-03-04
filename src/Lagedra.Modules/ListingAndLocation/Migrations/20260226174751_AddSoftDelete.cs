using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListingAndLocation.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "listings",
                table: "safety_device_definitions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "listings",
                table: "safety_device_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Bathrooms",
                schema: "listings",
                table: "listings",
                type: "numeric(3,1)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Bedrooms",
                schema: "listings",
                table: "listings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "listings",
                table: "listings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "listings",
                table: "listings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PropertyType",
                schema: "listings",
                table: "listings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SquareFootage",
                schema: "listings",
                table: "listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "listings",
                table: "consideration_definitions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "listings",
                table: "consideration_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "listings",
                table: "amenity_definitions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "listings",
                table: "amenity_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "listings",
                table: "safety_device_definitions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "listings",
                table: "safety_device_definitions");

            migrationBuilder.DropColumn(
                name: "Bathrooms",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "Bedrooms",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "PropertyType",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "SquareFootage",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "listings",
                table: "consideration_definitions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "listings",
                table: "consideration_definitions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "listings",
                table: "amenity_definitions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "listings",
                table: "amenity_definitions");
        }
    }
}
