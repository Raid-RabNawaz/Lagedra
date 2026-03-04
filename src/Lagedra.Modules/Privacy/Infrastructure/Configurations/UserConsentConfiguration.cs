using Lagedra.Modules.Privacy.Domain.Aggregates;
using Lagedra.Modules.Privacy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.Privacy.Infrastructure.Configurations;

public sealed class UserConsentConfiguration : IEntityTypeConfiguration<UserConsent>
{
    public void Configure(EntityTypeBuilder<UserConsent> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("user_consents");
        builder.HasKey(uc => uc.Id);

        builder.Property(uc => uc.UserId).IsRequired();
        builder.HasIndex(uc => uc.UserId).IsUnique();

        builder.HasMany(uc => uc.ConsentRecords)
            .WithOne()
            .HasForeignKey(cr => cr.UserConsentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(uc => uc.DomainEvents);
    }
}
