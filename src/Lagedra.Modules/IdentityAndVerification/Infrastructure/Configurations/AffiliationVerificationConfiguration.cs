using Lagedra.Modules.IdentityAndVerification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.IdentityAndVerification.Infrastructure.Configurations;

public sealed class AffiliationVerificationConfiguration : IEntityTypeConfiguration<AffiliationVerification>
{
    public void Configure(EntityTypeBuilder<AffiliationVerification> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("affiliation_verifications");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId).IsRequired();
        builder.HasIndex(a => a.UserId);

        builder.Property(a => a.OrganizationType).HasMaxLength(100);

        builder.Property(a => a.VerificationMethod)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Ignore(a => a.DomainEvents);
    }
}
