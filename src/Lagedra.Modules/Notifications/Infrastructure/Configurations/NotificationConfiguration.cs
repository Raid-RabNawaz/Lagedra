using Lagedra.Modules.Notifications.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.Notifications.Infrastructure.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.RecipientUserId).IsRequired();
        builder.HasIndex(n => n.RecipientUserId);

        builder.Property(n => n.RecipientEmail).HasMaxLength(320).IsRequired();

        builder.Property(n => n.Channel)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(n => n.TemplateId).HasMaxLength(100).IsRequired();

        builder.Property(n => n.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(n => n.Status);

        builder.Property(n => n.Payload)
            .HasColumnType("jsonb");

        builder.Property(n => n.LastError).HasMaxLength(2000);

        builder.Ignore(n => n.DomainEvents);
    }
}
