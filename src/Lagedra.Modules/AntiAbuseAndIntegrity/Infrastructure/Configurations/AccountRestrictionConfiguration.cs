using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Configurations;

public sealed class AccountRestrictionConfiguration : IEntityTypeConfiguration<AccountRestriction>
{
    public void Configure(EntityTypeBuilder<AccountRestriction> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("account_restrictions");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId).IsRequired();
        builder.HasIndex(r => r.UserId);

        builder.Property(r => r.RestrictionLevel)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.AppliedAt).IsRequired();

        builder.Property(r => r.Reason)
            .HasMaxLength(500)
            .IsRequired();
    }
}
