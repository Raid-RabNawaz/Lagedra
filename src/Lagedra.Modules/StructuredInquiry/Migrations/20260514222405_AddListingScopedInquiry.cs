using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StructuredInquiry.Migrations
{
    /// <inheritdoc />
    public partial class AddListingScopedInquiry : Migration
    {
        private static readonly string[] ListingTenantIndexColumns = ["ListingId", "TenantUserId"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            // Phase 17 — pre-booking inquiries don't have a deal yet, so DealId
            // becomes nullable. ListingId + TenantUserId become the new identity.
            migrationBuilder.AlterColumn<Guid>(
                name: "DealId",
                schema: "inquiry",
                table: "sessions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ListingId",
                schema: "inquiry",
                table: "sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantUserId",
                schema: "inquiry",
                table: "sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Backfill the new columns from activation_billing.deal_applications.
            // The join is one-to-one because every legacy session already had
            // a DealId. Sessions with no matching application (shouldn't exist
            // in production but defensive) will keep the zero-guid default
            // — they'll be cleaned up by the integrity scan job. The backfill
            // is wrapped in a DO block so the migration still succeeds when
            // ActivationAndBilling hasn't been deployed to this database yet
            // (fresh dev envs that only run a subset of modules); without the
            // table the backfill is just a no-op rather than a hard failure.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'activation_billing'
                          AND table_name   = 'deal_applications'
                    ) THEN
                        UPDATE inquiry.sessions s
                        SET    "ListingId"    = a."ListingId",
                               "TenantUserId" = a."TenantUserId"
                        FROM   activation_billing.deal_applications a
                        WHERE  a."DealId" = s."DealId";
                    END IF;
                END
                $$;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_sessions_ListingId",
                schema: "inquiry",
                table: "sessions",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_ListingId_TenantUserId",
                schema: "inquiry",
                table: "sessions",
                columns: ListingTenantIndexColumns);

            migrationBuilder.AddColumn<string>(
                name: "OpenQuestionText",
                schema: "inquiry",
                table: "questions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropColumn(
                name: "OpenQuestionText",
                schema: "inquiry",
                table: "questions");

            migrationBuilder.DropIndex(
                name: "IX_sessions_ListingId_TenantUserId",
                schema: "inquiry",
                table: "sessions");

            migrationBuilder.DropIndex(
                name: "IX_sessions_ListingId",
                schema: "inquiry",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "TenantUserId",
                schema: "inquiry",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "ListingId",
                schema: "inquiry",
                table: "sessions");

            // Drop sessions whose DealId is null before tightening the column.
            // These can only be Phase 17 pre-booking sessions; without a deal
            // they have no place in the legacy schema.
            migrationBuilder.Sql("""
                DELETE FROM inquiry.sessions WHERE "DealId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "DealId",
                schema: "inquiry",
                table: "sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
