using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Configurations;

public sealed class DealPaymentConfirmationConfiguration
    : IEntityTypeConfiguration<DealPaymentConfirmation>
{
    public void Configure(EntityTypeBuilder<DealPaymentConfirmation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("deal_payment_confirmations");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.DealId).IsRequired();
        builder.HasIndex(c => c.DealId).IsUnique();

        builder.Property(c => c.TotalTenantPaymentCents).IsRequired();
        builder.Property(c => c.TotalHostPlatformPaymentCents).IsRequired();
        builder.Property(c => c.FirstMonthRentCents).IsRequired().HasDefaultValue(0L);
        builder.Property(c => c.DepositAmountCents).IsRequired().HasDefaultValue(0L);
        builder.Property(c => c.InsuranceFeeCents).IsRequired().HasDefaultValue(0L);
        builder.Property(c => c.MonthlyProtocolFeeCents).IsRequired().HasDefaultValue(0L);
        builder.Property(c => c.ServiceFeeCents).IsRequired().HasDefaultValue(0L);
        builder.Property(c => c.HostPaidPlatform).IsRequired();

        builder.Property(c => c.HostConfirmed).IsRequired();
        builder.Property(c => c.TenantDisputed).IsRequired();

        builder.Property(c => c.DisputeReason).HasMaxLength(2000);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.GracePeriodExpiresAt).IsRequired();

        builder.Property(c => c.StripePaymentIntentId).HasMaxLength(255);
        builder.HasIndex(c => c.StripePaymentIntentId);
        builder.Property(c => c.StripePaymentStatus).HasMaxLength(50);

        builder.Property(c => c.TruthSurfaceSnapshotId);
        builder.HasIndex(c => c.TruthSurfaceSnapshotId);

        // Deposit return handshake (non-custodial, host-held).
        builder.Property(c => c.MoveOutInitiatedAt);
        builder.Property(c => c.MoveOutInitiatedByUserId);
        builder.Property(c => c.HostConfirmedDepositReturnedAt);
        builder.Property(c => c.TenantConfirmedDepositReceivedAt);
        builder.Property(c => c.DepositReturnAmountCents);
        builder.Property(c => c.DepositReturnMethod).HasMaxLength(50);
        builder.Property(c => c.DepositReturnNote).HasMaxLength(2000);
        builder.Property(c => c.DepositReturnSettledAt);
        builder.Property(c => c.DepositReturnReminderSentAt);

        builder.HasIndex(c => c.Status);

        builder.Ignore(c => c.DomainEvents);
    }
}
