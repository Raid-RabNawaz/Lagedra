using Lagedra.Modules.PartnerNetwork.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.PartnerNetwork.Infrastructure.Configurations;

public sealed class PartnerMemberConfiguration : IEntityTypeConfiguration<PartnerMember>
{
    public void Configure(EntityTypeBuilder<PartnerMember> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("partner_members");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.OrganizationId).IsRequired();
        builder.Property(m => m.UserId).IsRequired();

        builder.Property(m => m.MemberRole)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(m => m.JoinedAt).IsRequired();

        builder.HasIndex(m => new { m.OrganizationId, m.UserId }).IsUnique();
        builder.HasIndex(m => m.UserId);
    }
}
