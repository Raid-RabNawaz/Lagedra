using Lagedra.Modules.PartnerNetwork.Domain.Aggregates;
using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.PartnerNetwork.Infrastructure.Configurations;

public sealed class PartnerEndorsementConfiguration
    : IEntityTypeConfiguration<PartnerEndorsement>
{
    public void Configure(EntityTypeBuilder<PartnerEndorsement> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("partner_endorsements");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.OrganizationId).IsRequired();
        builder.Property(e => e.TenantUserId).IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.RequestedAt).IsRequired();
        builder.Property(e => e.RequestedByUserId).IsRequired();

        builder.Property(e => e.ApprovedAt);
        builder.Property(e => e.ApprovedByUserId);

        builder.Property(e => e.RevokedAt);
        builder.Property(e => e.RevokedByUserId);
        builder.Property(e => e.RevokeReason).HasMaxLength(2000);

        builder.Property(e => e.ExpiresAt);

        builder.Property(e => e.Note).HasMaxLength(2000);

        builder.HasIndex(e => new { e.OrganizationId, e.Status });
        builder.HasIndex(e => new { e.TenantUserId, e.Status });
        builder.HasIndex(e => e.ExpiresAt);

        // Partial unique index: at most one Requested-or-Approved row per (org, tenant).
        // Terminal rows (Revoked / Expired) are excluded so history can accumulate.
        builder.HasIndex(e => new { e.OrganizationId, e.TenantUserId })
            .HasDatabaseName("ix_partner_endorsements_active_per_tenant_per_org")
            .IsUnique()
            .HasFilter("\"Status\" IN ('Requested', 'Approved')");

        builder.Ignore(e => e.DomainEvents);
    }
}
