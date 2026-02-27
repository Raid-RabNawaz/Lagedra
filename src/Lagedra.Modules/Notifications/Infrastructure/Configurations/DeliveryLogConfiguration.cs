using Lagedra.Modules.Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.Notifications.Infrastructure.Configurations;

public sealed class DeliveryLogConfiguration : IEntityTypeConfiguration<DeliveryLog>
{
    public void Configure(EntityTypeBuilder<DeliveryLog> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("delivery_logs");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.NotificationId).IsRequired();
        builder.HasIndex(d => d.NotificationId);

        builder.Property(d => d.BrevoMessageId).HasMaxLength(200);
        builder.Property(d => d.Error).HasMaxLength(2000);
    }
}
