using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityAndVerification.Migrations
{
    /// <inheritdoc />
    public partial class AddScreeningOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "screening_orders",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    HostUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ExternalOrderId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ExternalReportId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ApplicationUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StripePaymentIntentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_screening_orders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_screening_orders_ApplicationId",
                schema: "identity",
                table: "screening_orders",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_screening_orders_ExternalOrderId",
                schema: "identity",
                table: "screening_orders",
                column: "ExternalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_screening_orders_StripePaymentIntentId",
                schema: "identity",
                table: "screening_orders",
                column: "StripePaymentIntentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "screening_orders",
                schema: "identity");
        }
    }
}
