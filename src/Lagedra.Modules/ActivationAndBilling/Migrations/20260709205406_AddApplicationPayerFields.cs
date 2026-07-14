using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActivationAndBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationPayerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayerType",
                schema: "activation_billing",
                table: "deal_applications",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Tenant");

            migrationBuilder.AddColumn<Guid>(
                name: "PayerUserId",
                schema: "activation_billing",
                table: "deal_applications",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayerType",
                schema: "activation_billing",
                table: "deal_applications");

            migrationBuilder.DropColumn(
                name: "PayerUserId",
                schema: "activation_billing",
                table: "deal_applications");
        }
    }
}
