using Lagedra.Modules.Privacy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.Privacy.Infrastructure.Configurations;

public sealed class LegalHoldConfiguration : IEntityTypeConfiguration<LegalHold>
{
    public void Configure(EntityTypeBuilder<LegalHold> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("legal_holds");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.UserId).IsRequired();
        builder.HasIndex(h => h.UserId);

        builder.Property(h => h.Reason).HasMaxLength(500).IsRequired();
    }
}
