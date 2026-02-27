using Lagedra.Modules.Arbitration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.Arbitration.Infrastructure.Configurations;

public sealed class EvidenceSlotConfiguration : IEntityTypeConfiguration<EvidenceSlot>
{
    public void Configure(EntityTypeBuilder<EvidenceSlot> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("evidence_slots");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CaseId).IsRequired();
        builder.Property(e => e.SlotType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.SubmittedBy).IsRequired();
        builder.Property(e => e.FileReference).HasMaxLength(500).IsRequired();
        builder.Property(e => e.SubmittedAt).IsRequired();
    }
}
