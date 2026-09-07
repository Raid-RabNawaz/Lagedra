using Lagedra.Modules.Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.Notifications.Infrastructure.Configurations;

public sealed class SmsConsentConfiguration : IEntityTypeConfiguration<SmsConsent>
{
    public void Configure(EntityTypeBuilder<SmsConsent> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("sms_consents");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.PhoneE164).HasMaxLength(20).IsRequired();
        builder.HasIndex(c => c.PhoneE164).IsUnique();
        builder.HasIndex(c => c.UserId);

        builder.Property(c => c.Source).HasMaxLength(50).IsRequired();
        builder.Property(c => c.OptedIn).IsRequired();
    }
}
