using Lagedra.Modules.IdentityAndVerification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.IdentityAndVerification.Infrastructure.Configurations;

public sealed class BackgroundCheckReportConfiguration : IEntityTypeConfiguration<BackgroundCheckReport>
{
    public void Configure(EntityTypeBuilder<BackgroundCheckReport> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("background_check_reports");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId).IsRequired();
        builder.HasIndex(r => r.UserId);

        builder.Property(r => r.ExternalReportId)
            .HasColumnName("PersonaReportId")
            .HasMaxLength(200);

        builder.Property(r => r.Result)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.ReceivedAt).IsRequired();
        builder.Property(r => r.ExpiresAt).IsRequired();

        builder.Ignore(r => r.DomainEvents);
    }
}
