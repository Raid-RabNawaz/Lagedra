using Lagedra.Modules.IdentityAndVerification.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.IdentityAndVerification.Infrastructure.Configurations;

public sealed class IdentityProfileConfiguration : IEntityTypeConfiguration<IdentityProfile>
{
    public void Configure(EntityTypeBuilder<IdentityProfile> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("identity_profiles");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId).IsRequired();
        builder.HasIndex(p => p.UserId).IsUnique();

        builder.Property(p => p.FirstName).HasMaxLength(200);
        builder.Property(p => p.LastName).HasMaxLength(200);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.VerificationClass)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Ignore(p => p.DomainEvents);
    }
}
