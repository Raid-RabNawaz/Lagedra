using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Configurations;

public sealed class AbuseCaseConfiguration : IEntityTypeConfiguration<AbuseCase>
{
    public void Configure(EntityTypeBuilder<AbuseCase> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("abuse_cases");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.SubjectUserId).IsRequired();
        builder.HasIndex(a => a.SubjectUserId);

        builder.Property(a => a.AbuseType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.DetectedAt).IsRequired();

        builder.Ignore(a => a.DomainEvents);
    }
}
