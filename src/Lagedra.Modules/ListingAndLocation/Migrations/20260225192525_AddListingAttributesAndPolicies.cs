using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListingAndLocation.Migrations
{
    /// <inheritdoc />
    public partial class AddListingAttributesAndPolicies : Migration
    {
        private static readonly string[] amenity_definiation = new[] { "Category", "SortOrder" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cancellation_custom_terms",
                schema: "listings",
                table: "listings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cancellation_free_days",
                schema: "listings",
                table: "listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cancellation_partial_refund_days",
                schema: "listings",
                table: "listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cancellation_partial_refund_percent",
                schema: "listings",
                table: "listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_policy_type",
                schema: "listings",
                table: "listings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "house_rules_additional_rules",
                schema: "listings",
                table: "listings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "house_rules_check_in_time",
                schema: "listings",
                table: "listings",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "house_rules_check_out_time",
                schema: "listings",
                table: "listings",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "house_rules_leaving_instructions",
                schema: "listings",
                table: "listings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "house_rules_max_guests",
                schema: "listings",
                table: "listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "house_rules_parties_allowed",
                schema: "listings",
                table: "listings",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "house_rules_pets_allowed",
                schema: "listings",
                table: "listings",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "house_rules_pets_notes",
                schema: "listings",
                table: "listings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "house_rules_quiet_hours_end",
                schema: "listings",
                table: "listings",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "house_rules_quiet_hours_start",
                schema: "listings",
                table: "listings",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "house_rules_smoking_allowed",
                schema: "listings",
                table: "listings",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "amenity_definitions",
                schema: "listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IconKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_amenity_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "consideration_definitions",
                schema: "listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IconKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consideration_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "safety_device_definitions",
                schema: "listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IconKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_safety_device_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "listing_amenities",
                schema: "listings",
                columns: table => new
                {
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmenityDefinitionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listing_amenities", x => new { x.ListingId, x.AmenityDefinitionId });
                    table.ForeignKey(
                        name: "FK_listing_amenities_amenity_definitions_AmenityDefinitionId",
                        column: x => x.AmenityDefinitionId,
                        principalSchema: "listings",
                        principalTable: "amenity_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_listing_amenities_listings_ListingId",
                        column: x => x.ListingId,
                        principalSchema: "listings",
                        principalTable: "listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "listing_considerations",
                schema: "listings",
                columns: table => new
                {
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsiderationDefinitionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listing_considerations", x => new { x.ListingId, x.ConsiderationDefinitionId });
                    table.ForeignKey(
                        name: "FK_listing_considerations_consideration_definitions_Considerat~",
                        column: x => x.ConsiderationDefinitionId,
                        principalSchema: "listings",
                        principalTable: "consideration_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_listing_considerations_listings_ListingId",
                        column: x => x.ListingId,
                        principalSchema: "listings",
                        principalTable: "listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "listing_safety_devices",
                schema: "listings",
                columns: table => new
                {
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    SafetyDeviceDefinitionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listing_safety_devices", x => new { x.ListingId, x.SafetyDeviceDefinitionId });
                    table.ForeignKey(
                        name: "FK_listing_safety_devices_listings_ListingId",
                        column: x => x.ListingId,
                        principalSchema: "listings",
                        principalTable: "listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_listing_safety_devices_safety_device_definitions_SafetyDevi~",
                        column: x => x.SafetyDeviceDefinitionId,
                        principalSchema: "listings",
                        principalTable: "safety_device_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_amenity_definitions_Category_SortOrder",
                schema: "listings",
                table: "amenity_definitions",
                columns: amenity_definiation);

            migrationBuilder.CreateIndex(
                name: "IX_amenity_definitions_Name",
                schema: "listings",
                table: "amenity_definitions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_consideration_definitions_Name",
                schema: "listings",
                table: "consideration_definitions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_consideration_definitions_SortOrder",
                schema: "listings",
                table: "consideration_definitions",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_listing_amenities_AmenityDefinitionId",
                schema: "listings",
                table: "listing_amenities",
                column: "AmenityDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_listing_considerations_ConsiderationDefinitionId",
                schema: "listings",
                table: "listing_considerations",
                column: "ConsiderationDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_listing_safety_devices_SafetyDeviceDefinitionId",
                schema: "listings",
                table: "listing_safety_devices",
                column: "SafetyDeviceDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_safety_device_definitions_Name",
                schema: "listings",
                table: "safety_device_definitions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_safety_device_definitions_SortOrder",
                schema: "listings",
                table: "safety_device_definitions",
                column: "SortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "listing_amenities",
                schema: "listings");

            migrationBuilder.DropTable(
                name: "listing_considerations",
                schema: "listings");

            migrationBuilder.DropTable(
                name: "listing_safety_devices",
                schema: "listings");

            migrationBuilder.DropTable(
                name: "amenity_definitions",
                schema: "listings");

            migrationBuilder.DropTable(
                name: "consideration_definitions",
                schema: "listings");

            migrationBuilder.DropTable(
                name: "safety_device_definitions",
                schema: "listings");

            migrationBuilder.DropColumn(
                name: "cancellation_custom_terms",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "cancellation_free_days",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "cancellation_partial_refund_days",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "cancellation_partial_refund_percent",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "cancellation_policy_type",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "house_rules_additional_rules",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "house_rules_check_in_time",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "house_rules_check_out_time",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "house_rules_leaving_instructions",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "house_rules_max_guests",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "house_rules_parties_allowed",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "house_rules_pets_allowed",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "house_rules_pets_notes",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "house_rules_quiet_hours_end",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "house_rules_quiet_hours_start",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "house_rules_smoking_allowed",
                schema: "listings",
                table: "listings");
        }
    }
}
