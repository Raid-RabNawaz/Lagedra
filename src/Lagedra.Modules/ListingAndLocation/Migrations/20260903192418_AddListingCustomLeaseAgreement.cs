using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListingAndLocation.Migrations
{
    /// <inheritdoc />
    public partial class AddListingCustomLeaseAgreement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LeaseAgreementSource",
                schema: "listings",
                table: "listings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "LagedraTemplate");

            migrationBuilder.AddColumn<string>(
                name: "custom_lease_content_hash",
                schema: "listings",
                table: "listings",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "custom_lease_content_type",
                schema: "listings",
                table: "listings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "custom_lease_file_name",
                schema: "listings",
                table: "listings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "custom_lease_size_bytes",
                schema: "listings",
                table: "listings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "custom_lease_storage_key",
                schema: "listings",
                table: "listings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "custom_lease_uploaded_at",
                schema: "listings",
                table: "listings",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LeaseAgreementSource",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "custom_lease_content_hash",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "custom_lease_content_type",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "custom_lease_file_name",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "custom_lease_size_bytes",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "custom_lease_storage_key",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "custom_lease_uploaded_at",
                schema: "listings",
                table: "listings");
        }
    }
}
