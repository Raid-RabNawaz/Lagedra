using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Configurations;

public sealed class CollusionPatternConfiguration : IEntityTypeConfiguration<CollusionPattern>
{
    public void Configure(EntityTypeBuilder<CollusionPattern> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("collusion_patterns");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.AbuseCaseId).IsRequired();
        builder.HasIndex(c => c.AbuseCaseId);

        builder.Property(c => c.PartyAUserId).IsRequired();
        builder.Property(c => c.PartyBUserId).IsRequired();
        builder.Property(c => c.RepeatedDealCount).IsRequired();
        builder.Property(c => c.FirstOccurrence).IsRequired();
        builder.Property(c => c.LatestOccurrence).IsRequired();
    }
}
