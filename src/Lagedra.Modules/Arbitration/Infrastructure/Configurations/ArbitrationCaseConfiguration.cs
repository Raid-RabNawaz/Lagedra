using Lagedra.Modules.Arbitration.Domain.Aggregates;
using Lagedra.Modules.Arbitration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.Arbitration.Infrastructure.Configurations;

public sealed class ArbitrationCaseConfiguration : IEntityTypeConfiguration<ArbitrationCase>
{
    public void Configure(EntityTypeBuilder<ArbitrationCase> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("arbitration_cases");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.DealId).IsRequired();
        builder.HasIndex(c => c.DealId);

        builder.Property(c => c.Tier)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.Category)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(c => c.Status);

        builder.Property(c => c.FiledAt).IsRequired();
        builder.Property(c => c.DecisionSummary).HasMaxLength(4000);
        builder.Property(c => c.AwardAmount).HasPrecision(18, 2);

        builder.HasMany(c => c.EvidenceSlots)
            .WithOne()
            .HasForeignKey(e => e.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.ArbitratorAssignments)
            .WithOne()
            .HasForeignKey(a => a.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.EvidenceSlots).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(c => c.ArbitratorAssignments).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(c => c.DomainEvents);
    }
}
