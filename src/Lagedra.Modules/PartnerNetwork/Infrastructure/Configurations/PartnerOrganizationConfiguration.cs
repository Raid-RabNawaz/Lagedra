using Lagedra.Modules.PartnerNetwork.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.PartnerNetwork.Infrastructure.Configurations;

public sealed class PartnerOrganizationConfiguration
    : IEntityTypeConfiguration<PartnerOrganization>
{
    public void Configure(EntityTypeBuilder<PartnerOrganization> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("partner_organizations");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name).HasMaxLength(500).IsRequired();
        builder.HasIndex(o => o.Name).IsUnique();

        builder.Property(o => o.OrganizationType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(o => o.ContactEmail).HasMaxLength(500).IsRequired();
        builder.Property(o => o.TaxId).HasMaxLength(100);
        builder.Property(o => o.SuspensionReason).HasMaxLength(2000);

        builder.HasIndex(o => o.Status);

        builder.Ignore(o => o.DomainEvents);
    }
}
