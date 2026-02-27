using Lagedra.Modules.IdentityAndVerification.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.IdentityAndVerification.Infrastructure.Configurations;

public sealed class VerificationCaseConfiguration : IEntityTypeConfiguration<VerificationCase>
{
    public void Configure(EntityTypeBuilder<VerificationCase> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("verification_cases");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId).IsRequired();
        builder.HasIndex(c => c.UserId);

        builder.Property(c => c.PersonaInquiryId).HasMaxLength(200);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Ignore(c => c.DomainEvents);
    }
}
