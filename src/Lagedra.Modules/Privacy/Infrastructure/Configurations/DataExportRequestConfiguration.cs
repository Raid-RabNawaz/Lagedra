using Lagedra.Modules.Privacy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.Privacy.Infrastructure.Configurations;

public sealed class DataExportRequestConfiguration : IEntityTypeConfiguration<DataExportRequest>
{
    public void Configure(EntityTypeBuilder<DataExportRequest> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("data_export_requests");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserId).IsRequired();
        builder.HasIndex(e => e.UserId);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.PackageUrl).HasMaxLength(2048);
    }
}
