using Lagedra.Modules.Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.Notifications.Infrastructure.Configurations;

public sealed class InAppNotificationConfiguration : IEntityTypeConfiguration<InAppNotification>
{
    public void Configure(EntityTypeBuilder<InAppNotification> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("in_app_notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.RecipientUserId).IsRequired();
        builder.Property(n => n.Title).HasMaxLength(500).IsRequired();
        builder.Property(n => n.Body).HasMaxLength(4000).IsRequired();
        builder.Property(n => n.Category).HasMaxLength(100).IsRequired();
        builder.Property(n => n.RelatedEntityType).HasMaxLength(100);

        builder.HasIndex(n => new { n.RecipientUserId, n.IsRead });
        builder.HasIndex(n => n.CreatedAt);
    }
}
