using Lagedra.Modules.PartnerNetwork.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.PartnerNetwork.Infrastructure.Configurations;

public sealed class ReferralRedemptionConfiguration : IEntityTypeConfiguration<ReferralRedemption>
{
    public void Configure(EntityTypeBuilder<ReferralRedemption> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("referral_redemptions");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReferralLinkId).IsRequired();
        builder.Property(r => r.OrganizationId).IsRequired();
        builder.Property(r => r.RedeemedByUserId).IsRequired();
        builder.Property(r => r.RedeemedAt).IsRequired();

        builder.HasIndex(r => new { r.ReferralLinkId, r.RedeemedByUserId }).IsUnique();
        builder.HasIndex(r => r.RedeemedByUserId);
    }
}
