using Lagedra.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Auth.Infrastructure.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.IsActive)
            .HasDefaultValue(false);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.LastLoginAt)
            .IsRequired(false);

        // Phase 16.9: cached Stripe customer id, lazily populated by the
        // booking pre-flight. Capped to a generous bound that comfortably
        // fits Stripe's `cus_…` ids; nullable for legacy users created
        // before card-on-file was introduced.
        builder.Property(u => u.StripeCustomerId)
            .HasMaxLength(64)
            .IsRequired(false);
    }
}
