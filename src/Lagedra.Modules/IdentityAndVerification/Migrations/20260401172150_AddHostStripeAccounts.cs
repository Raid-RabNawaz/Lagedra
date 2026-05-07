using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityAndVerification.Migrations
{
    /// <inheritdoc />
    public partial class AddHostStripeAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "host_stripe_accounts",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HostUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StripeAccountId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OnboardingStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ChargesEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PayoutsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_host_stripe_accounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_host_stripe_accounts_HostUserId",
                schema: "identity",
                table: "host_stripe_accounts",
                column: "HostUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_host_stripe_accounts_StripeAccountId",
                schema: "identity",
                table: "host_stripe_accounts",
                column: "StripeAccountId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "host_stripe_accounts",
                schema: "identity");
        }
    }
}
