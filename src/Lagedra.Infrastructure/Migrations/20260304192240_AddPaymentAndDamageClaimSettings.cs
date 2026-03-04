using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // EF Core migration scaffolded code

namespace Lagedra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentAndDamageClaimSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "platform",
                table: "platform_settings",
                columns: new[] { "Key", "Description", "UpdatedAt", "UpdatedByUserId", "Value" },
                values: new object[,]
                {
                    { "cancellation.insurance_refund_deadline_days", "Days after cancellation within which insurance premium refund is eligible", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "30" },
                    { "damage_claim.filing_deadline_days", "Days after check-out within which a damage claim can be filed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "14" },
                    { "host_platform_payment.reminder_interval_days", "Days between reminder emails to host for unpaid platform fees", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "2" },
                    { "host_platform_payment.suspend_after_days", "Days after host confirms tenant payment to suspend host if platform fee not paid", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "14" },
                    { "payment.auto_cancel_after_days", "Days after booking confirmation to auto-cancel if tenant has not paid", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "7" },
                    { "payment.grace_period_days", "Days after booking confirmation before payment is considered overdue", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "3" },
                    { "payment.reminder_after_days", "Days after booking confirmation to send payment reminder to tenant", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "4" }
                });
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
