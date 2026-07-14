using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListingAndLocation.Migrations
{
    /// <inheritdoc />
    public partial class AddListingLeaseTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "lease_built_before_1978",
                schema: "listings",
                table: "listings",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "lease_early_termination_fee_months",
                schema: "listings",
                table: "listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "lease_furnished",
                schema: "listings",
                table: "listings",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lease_included_appliances",
                schema: "listings",
                table: "listings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "lease_key_count",
                schema: "listings",
                table: "listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "lease_key_replacement_fee_cents",
                schema: "listings",
                table: "listings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "lease_late_fee_grace_days",
                schema: "listings",
                table: "listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "lease_late_fee_percent",
                schema: "listings",
                table: "listings",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lease_lead_paint_knowledge",
                schema: "listings",
                table: "listings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "lease_lockout_fee_cents",
                schema: "listings",
                table: "listings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "lease_mailbox_key_count",
                schema: "listings",
                table: "listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "lease_max_guest_consecutive_days",
                schema: "listings",
                table: "listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "lease_nsf_first_fee_cents",
                schema: "listings",
                table: "listings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "lease_nsf_subsequent_fee_cents",
                schema: "listings",
                table: "listings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lease_parking_description",
                schema: "listings",
                table: "listings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "lease_parking_included_in_rent",
                schema: "listings",
                table: "listings",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "lease_parking_space_count",
                schema: "listings",
                table: "listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lease_payment_methods",
                schema: "listings",
                table: "listings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "lease_rent_cap_just_cause_exempt",
                schema: "listings",
                table: "listings",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "lease_rent_due_day",
                schema: "listings",
                table: "listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "lease_renters_insurance_min_cents",
                schema: "listings",
                table: "listings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lease_utilities_responsibility",
                schema: "listings",
                table: "listings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "lease_yard_maintenance_by_tenant",
                schema: "listings",
                table: "listings",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "lease_built_before_1978",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "lease_early_termination_fee_months",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "lease_furnished",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "lease_included_appliances",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "lease_key_count",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "lease_key_replacement_fee_cents",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "lease_late_fee_grace_days",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "lease_late_fee_percent",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "lease_lead_paint_knowledge",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "lease_lockout_fee_cents",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "lease_mailbox_key_count",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "lease_max_guest_consecutive_days",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "lease_nsf_first_fee_cents",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "lease_nsf_subsequent_fee_cents",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "lease_parking_description",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "lease_parking_included_in_rent",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "lease_parking_space_count",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "lease_payment_methods",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "lease_rent_cap_just_cause_exempt",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "lease_rent_due_day",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "lease_renters_insurance_min_cents",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "lease_utilities_responsibility",
                schema: "listings",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "lease_yard_maintenance_by_tenant",
                schema: "listings",
                table: "listings");
        }
    }
}
