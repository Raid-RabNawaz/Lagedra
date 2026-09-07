using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaseAgreements.Migrations
{
    /// <inheritdoc />
    public partial class AddDealLeaseDocumentSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "TemplateVersionId",
                schema: "lease_agreements",
                table: "deal_lease_documents",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "TemplateId",
                schema: "lease_agreements",
                table: "deal_lease_documents",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                schema: "lease_agreements",
                table: "deal_lease_documents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "LagedraTemplate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                schema: "lease_agreements",
                table: "deal_lease_documents");

            migrationBuilder.AlterColumn<Guid>(
                name: "TemplateVersionId",
                schema: "lease_agreements",
                table: "deal_lease_documents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "TemplateId",
                schema: "lease_agreements",
                table: "deal_lease_documents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
