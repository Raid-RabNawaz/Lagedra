using Lagedra.Modules.IdentityAndVerification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.IdentityAndVerification.Infrastructure.Configurations;

public sealed class HostStripeAccountConfiguration : IEntityTypeConfiguration<HostStripeAccount>
{
    public void Configure(EntityTypeBuilder<HostStripeAccount> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("host_stripe_accounts");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.HostUserId).IsRequired();
        builder.HasIndex(h => h.HostUserId).IsUnique();

        builder.Property(h => h.StripeAccountId)
            .IsRequired()
            .HasMaxLength(255);
        builder.HasIndex(h => h.StripeAccountId).IsUnique();

        builder.Property(h => h.OnboardingStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(h => h.ChargesEnabled).IsRequired();
        builder.Property(h => h.PayoutsEnabled).IsRequired();
    }
}
