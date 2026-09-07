using Lagedra.Modules.LeaseAgreements.Domain.Aggregates;
using Lagedra.Modules.LeaseAgreements.Domain.Entities;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.LeaseAgreements.Infrastructure.Configurations;

public sealed class LeaseAgreementTemplateConfiguration : IEntityTypeConfiguration<LeaseAgreementTemplate>
{
    public void Configure(EntityTypeBuilder<LeaseAgreementTemplate> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("lease_templates");
        builder.HasKey(t => t.Id);

        builder.OwnsOne(t => t.JurisdictionCode, jc =>
        {
            jc.Property(c => c.Code)
                .HasColumnName("jurisdiction_code")
                .HasMaxLength(20)
                .IsRequired();

            jc.HasIndex(c => c.Code).IsUnique();
        });

        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
        builder.Property(t => t.ActiveVersionId);

        builder.HasMany(t => t.Versions)
            .WithOne()
            .HasForeignKey(v => v.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.Versions).HasField("_versions").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(t => t.DomainEvents);
    }
}

public sealed class LeaseTemplateVersionConfiguration : IEntityTypeConfiguration<LeaseTemplateVersion>
{
    public void Configure(EntityTypeBuilder<LeaseTemplateVersion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("lease_template_versions");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.VersionNumber).IsRequired();
        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(v => v.BodyHtml).HasColumnType("text").IsRequired();
        builder.HasIndex(v => new { v.TemplateId, v.VersionNumber }).IsUnique();
    }
}

public sealed class DealLeaseDocumentEntityConfiguration : IEntityTypeConfiguration<DealLeaseDocumentEntity>
{
    public void Configure(EntityTypeBuilder<DealLeaseDocumentEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("deal_lease_documents");
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => d.DealId).IsUnique();
        builder.Property(d => d.FileName).HasMaxLength(260).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Content).HasColumnType("bytea").IsRequired();
        builder.Property(d => d.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(d => d.Source)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(DealLeaseDocumentSource.LagedraTemplate)
            .IsRequired();
    }
}
