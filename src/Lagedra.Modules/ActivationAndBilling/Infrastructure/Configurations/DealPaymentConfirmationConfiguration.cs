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
        builder.Property(c => c.HostPaidPlatform).IsRequired();

        builder.Property(c => c.HostConfirmed).IsRequired();
        builder.Property(c => c.TenantDisputed).IsRequired();

        builder.Property(c => c.DisputeReason).HasMaxLength(2000);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.GracePeriodExpiresAt).IsRequired();

        builder.HasIndex(c => c.Status);

        builder.Ignore(c => c.DomainEvents);
    }
}
