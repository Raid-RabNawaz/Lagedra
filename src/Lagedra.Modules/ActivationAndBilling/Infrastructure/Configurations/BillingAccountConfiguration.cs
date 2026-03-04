using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Configurations;

public sealed class BillingAccountConfiguration : IEntityTypeConfiguration<BillingAccount>
{
    public void Configure(EntityTypeBuilder<BillingAccount> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("billing_accounts");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.DealId).IsRequired();
        builder.HasIndex(b => b.DealId).IsUnique();

        builder.Property(b => b.LandlordUserId).IsRequired();
        builder.Property(b => b.TenantUserId).IsRequired();

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.StartDate).IsRequired();

        builder.Property(b => b.StripeCustomerId).HasMaxLength(255);
        builder.Property(b => b.StripeSubscriptionId).HasMaxLength(255);

        builder.HasMany(b => b.Invoices)
            .WithOne()
            .HasForeignKey(i => i.BillingAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(b => b.DomainEvents);
    }
}
