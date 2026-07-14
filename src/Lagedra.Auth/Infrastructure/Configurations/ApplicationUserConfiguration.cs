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

        // Sign-up flow metadata (founding-partner / pre-launch join flow).
        builder.Property(u => u.CompanyName).HasMaxLength(200).IsRequired(false);
        builder.Property(u => u.SignupType).HasMaxLength(20).IsRequired(false);
        builder.Property(u => u.PortfolioSize).HasMaxLength(20).IsRequired(false);
        builder.Property(u => u.HousingType).HasMaxLength(50).IsRequired(false);
        builder.Property(u => u.PlacementsPerYear).HasMaxLength(20).IsRequired(false);
        builder.Property(u => u.IsPreLaunchSignup).HasDefaultValue(false);

        builder.Property(u => u.PhoneVerificationCodeHash).HasMaxLength(64).IsRequired(false);
        builder.Property(u => u.PhoneVerificationExpiresAt).IsRequired(false);
        builder.Property(u => u.PhoneVerificationSentAt).IsRequired(false);
        builder.Property(u => u.PhoneVerificationWindowStartedAt).IsRequired(false);
        builder.Property(u => u.PhoneVerificationSendCount).HasDefaultValue(0);

        builder.Property(u => u.MailingStreet).HasMaxLength(200).IsRequired(false);
        builder.Property(u => u.MailingCity).HasMaxLength(100).IsRequired(false);
        builder.Property(u => u.MailingState).HasMaxLength(50).IsRequired(false);
        builder.Property(u => u.MailingZip).HasMaxLength(20).IsRequired(false);
        builder.Property(u => u.MailingCountry).HasMaxLength(100).IsRequired(false);
        builder.Property(u => u.NoticeAddressSameAsMailing).HasDefaultValue(true);
        builder.Property(u => u.NoticeStreet).HasMaxLength(200).IsRequired(false);
        builder.Property(u => u.NoticeCity).HasMaxLength(100).IsRequired(false);
        builder.Property(u => u.NoticeState).HasMaxLength(50).IsRequired(false);
        builder.Property(u => u.NoticeZip).HasMaxLength(20).IsRequired(false);
        builder.Property(u => u.NoticeCountry).HasMaxLength(100).IsRequired(false);
        builder.Property(u => u.BrokerName).HasMaxLength(200).IsRequired(false);
        builder.Property(u => u.BrokerDreLicense).HasMaxLength(50).IsRequired(false);
        builder.Property(u => u.BrokerScopeNotes).HasMaxLength(2000).IsRequired(false);
    }
}
