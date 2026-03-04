using Lagedra.Modules.Privacy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.Privacy.Infrastructure.Configurations;

public sealed class DeletionRequestConfiguration : IEntityTypeConfiguration<DeletionRequest>
{
    public void Configure(EntityTypeBuilder<DeletionRequest> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("deletion_requests");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.UserId).IsRequired();
        builder.HasIndex(d => d.UserId);

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.BlockingReason).HasMaxLength(500);
    }
}
