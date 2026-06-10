using Lagedra.Modules.Arbitration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.Arbitration.Infrastructure.Configurations;

public sealed class DecisionPenaltyConfiguration : IEntityTypeConfiguration<DecisionPenalty>
{
    public void Configure(EntityTypeBuilder<DecisionPenalty> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("decision_penalties");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.CaseId).IsRequired();
        builder.HasIndex(p => p.CaseId);
        builder.Property(p => p.PartyUserId).IsRequired();
        builder.Property(p => p.PenaltyType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(p => p.AmountCents);
        builder.Property(p => p.Description).HasMaxLength(1000);
    }
}
