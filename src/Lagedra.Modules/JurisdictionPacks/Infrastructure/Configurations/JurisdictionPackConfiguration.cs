using Lagedra.Modules.JurisdictionPacks.Domain.Aggregates;
using Lagedra.Modules.JurisdictionPacks.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.JurisdictionPacks.Infrastructure.Configurations;

public sealed class JurisdictionPackConfiguration : IEntityTypeConfiguration<JurisdictionPack>
{
    public void Configure(EntityTypeBuilder<JurisdictionPack> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("jurisdiction_packs");
        builder.HasKey(p => p.Id);

        builder.OwnsOne(p => p.JurisdictionCode, jc =>
        {
            jc.Property(c => c.Code)
                .HasColumnName("jurisdiction_code")
                .HasMaxLength(20)
                .IsRequired();

            jc.HasIndex(c => c.Code).IsUnique();
        });

        builder.Property(p => p.ActiveVersionId);

        builder.HasMany(p => p.Versions)
            .WithOne()
            .HasForeignKey(v => v.PackId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(p => p.DomainEvents);
    }
}
