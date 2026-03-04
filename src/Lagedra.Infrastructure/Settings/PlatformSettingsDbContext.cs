using Lagedra.SharedKernel.Settings;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Infrastructure.Settings;

public sealed class PlatformSettingsDbContext(DbContextOptions<PlatformSettingsDbContext> options)
    : DbContext(options)
{
    public DbSet<PlatformSetting> PlatformSettings => Set<PlatformSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new PlatformSettingConfiguration());

        SeedDefaults(modelBuilder);
    }

    private static void SeedDefaults(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlatformSetting>().HasData(
            Seed(PlatformSettingKeys.ProtocolFeeMonthly, "7900",
                "Monthly protocol fee per active deal in cents (paid by host)"),
            Seed(PlatformSettingKeys.ProtocolFeePilotDiscount, "3900",
                "Pilot discount in cents subtracted from protocol fee"),
            Seed(PlatformSettingKeys.ProtocolFeePilotActive, "false",
                "Whether the pilot discount is currently active"),
            Seed(PlatformSettingKeys.ArbitrationFeeProtocolAdjudication, "4900",
                "Protocol adjudication filing fee in cents"),
            Seed(PlatformSettingKeys.ArbitrationFeeBindingArbitration, "9900",
                "Binding arbitration filing fee in cents"),

            Seed(PlatformSettingKeys.PaymentGracePeriodDays, "3",
                "Days after booking confirmation before payment is considered overdue"),
            Seed(PlatformSettingKeys.PaymentReminderAfterDays, "4",
                "Days after booking confirmation to send payment reminder to tenant"),
            Seed(PlatformSettingKeys.PaymentAutoCancelAfterDays, "7",
                "Days after booking confirmation to auto-cancel if tenant has not paid"),

            Seed(PlatformSettingKeys.HostPlatformPaymentReminderIntervalDays, "2",
                "Days between reminder emails to host for unpaid platform fees"),
            Seed(PlatformSettingKeys.HostPlatformPaymentSuspendAfterDays, "14",
                "Days after host confirms tenant payment to suspend host if platform fee not paid"),

            Seed(PlatformSettingKeys.CancellationInsuranceRefundDeadlineDays, "30",
                "Days after cancellation within which insurance premium refund is eligible"),
            Seed(PlatformSettingKeys.DamageClaimFilingDeadlineDays, "14",
                "Days after check-out within which a damage claim can be filed"));
    }

    private static object Seed(string key, string value, string description) => new
    {
        Key = key,
        Value = value,
        Description = description,
        UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedByUserId = (Guid?)null
    };
}
