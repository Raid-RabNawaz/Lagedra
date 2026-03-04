using Lagedra.Modules.Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.Notifications.Infrastructure.Configurations;

public sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("notification_templates");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TemplateId).HasMaxLength(100).IsRequired();
        builder.HasIndex(t => t.TemplateId).IsUnique();

        builder.Property(t => t.Channel)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Subject).HasMaxLength(500).IsRequired();
        builder.Property(t => t.HtmlBody).IsRequired();
        builder.Property(t => t.PlainTextBody);
    }
}
