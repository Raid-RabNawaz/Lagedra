using Lagedra.Modules.Evidence.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.Evidence.Infrastructure.Configurations;

public sealed class MetadataStrippingLogConfiguration : IEntityTypeConfiguration<MetadataStrippingLog>
{
    public void Configure(EntityTypeBuilder<MetadataStrippingLog> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("metadata_stripping_logs");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.UploadId).IsRequired();
        builder.HasIndex(l => l.UploadId);

        builder.Property(l => l.RemovedFields).IsRequired();
    }
}
