using Lagedra.SharedKernel.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Infrastructure.Settings;

public sealed class PlatformSettingConfiguration : IEntityTypeConfiguration<PlatformSetting>
{
    public void Configure(EntityTypeBuilder<PlatformSetting> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("platform_settings", "platform");

        builder.HasKey(s => s.Key);

        builder.Property(s => s.Key).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Value).HasMaxLength(1000).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.UpdatedAt).IsRequired();
        builder.Property(s => s.UpdatedByUserId);
    }
}
