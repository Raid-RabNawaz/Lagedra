using Lagedra.Modules.PartnerNetwork.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.PartnerNetwork.Infrastructure.Configurations;

public sealed class ReferralLinkConfiguration : IEntityTypeConfiguration<ReferralLink>
{
    public void Configure(EntityTypeBuilder<ReferralLink> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("referral_links");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.OrganizationId).IsRequired();
        builder.Property(l => l.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(l => l.Code).IsUnique();
        builder.Property(l => l.CreatedByUserId).IsRequired();
        builder.Property(l => l.UsageCount).IsRequired();
        builder.Property(l => l.IsActive).IsRequired();

        builder.HasIndex(l => l.OrganizationId);
    }
}
