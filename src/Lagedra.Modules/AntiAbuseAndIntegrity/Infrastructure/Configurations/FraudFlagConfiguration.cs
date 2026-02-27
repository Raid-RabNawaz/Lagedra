using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Configurations;

public sealed class FraudFlagConfiguration : IEntityTypeConfiguration<FraudFlag>
{
    public void Configure(EntityTypeBuilder<FraudFlag> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("fraud_flags");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.UserId).IsRequired();
        builder.HasIndex(f => f.UserId);

        builder.Property(f => f.FlagType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(f => f.Severity)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(f => f.FlaggedAt).IsRequired();
    }
}
