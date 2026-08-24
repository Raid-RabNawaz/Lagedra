using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lagedra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentAndDamageClaimSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ON CONFLICT DO NOTHING: rows may already exist (created at
            // runtime via the admin settings UI before this migration ran).
            migrationBuilder.Sql(
                """
                INSERT INTO platform.platform_settings ("Key", "Description", "UpdatedAt", "UpdatedByUserId", "Value")
                VALUES
                    ('cancellation.insurance_refund_deadline_days', 'Days after cancellation within which insurance premium refund is eligible', TIMESTAMPTZ '2026-01-01T00:00:00Z', NULL, '30'),
                    ('damage_claim.filing_deadline_days', 'Days after check-out within which a damage claim can be filed', TIMESTAMPTZ '2026-01-01T00:00:00Z', NULL, '14'),
                    ('host_platform_payment.reminder_interval_days', 'Days between reminder emails to host for unpaid platform fees', TIMESTAMPTZ '2026-01-01T00:00:00Z', NULL, '2'),
                    ('host_platform_payment.suspend_after_days', 'Days after host confirms tenant payment to suspend host if platform fee not paid', TIMESTAMPTZ '2026-01-01T00:00:00Z', NULL, '14'),
                    ('payment.auto_cancel_after_days', 'Days after booking confirmation to auto-cancel if tenant has not paid', TIMESTAMPTZ '2026-01-01T00:00:00Z', NULL, '7'),
                    ('payment.grace_period_days', 'Days after booking confirmation before payment is considered overdue', TIMESTAMPTZ '2026-01-01T00:00:00Z', NULL, '3'),
                    ('payment.reminder_after_days', 'Days after booking confirmation to send payment reminder to tenant', TIMESTAMPTZ '2026-01-01T00:00:00Z', NULL, '4')
                ON CONFLICT ("Key") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "platform",
                table: "platform_settings",
                keyColumn: "Key",
                keyValue: "cancellation.insurance_refund_deadline_days");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "platform_settings",
                keyColumn: "Key",
                keyValue: "damage_claim.filing_deadline_days");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "platform_settings",
                keyColumn: "Key",
                keyValue: "host_platform_payment.reminder_interval_days");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "platform_settings",
                keyColumn: "Key",
                keyValue: "host_platform_payment.suspend_after_days");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "platform_settings",
                keyColumn: "Key",
                keyValue: "payment.auto_cancel_after_days");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "platform_settings",
                keyColumn: "Key",
                keyValue: "payment.grace_period_days");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "platform_settings",
                keyColumn: "Key",
                keyValue: "payment.reminder_after_days");
        }
    }
}
