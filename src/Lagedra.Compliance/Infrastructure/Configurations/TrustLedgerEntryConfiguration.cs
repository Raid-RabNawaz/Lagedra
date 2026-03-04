using Lagedra.Compliance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Compliance.Infrastructure.Configurations;

public sealed class TrustLedgerEntryConfiguration : IEntityTypeConfiguration<TrustLedgerEntry>
{
    public void Configure(EntityTypeBuilder<TrustLedgerEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("trust_ledger_entries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserId).IsRequired();
        builder.HasIndex(e => e.UserId);

        builder.Property(e => e.EntryType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Description).HasMaxLength(1000);

        builder.HasIndex(e => e.ReferenceId);
        builder.HasIndex(e => e.OccurredAt);
    }
}
