using Lagedra.Modules.PartnerNetwork.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.PartnerNetwork.Infrastructure.Configurations;

public sealed class PartnerGuestInviteConfiguration
    : IEntityTypeConfiguration<PartnerGuestInvite>
{
    public void Configure(EntityTypeBuilder<PartnerGuestInvite> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("partner_guest_invites");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Email).HasMaxLength(500).IsRequired();
        builder.Property(i => i.FullName).HasMaxLength(500).IsRequired();
        builder.Property(i => i.OrganizationId).IsRequired();
        builder.Property(i => i.InvitedByUserId).IsRequired();
        builder.Property(i => i.InvitedUserId).IsRequired();

        builder.HasIndex(i => i.OrganizationId);
        builder.HasIndex(i => i.InvitedUserId);
        builder.HasIndex(i => new { i.OrganizationId, i.InvitedAt });
    }
}
