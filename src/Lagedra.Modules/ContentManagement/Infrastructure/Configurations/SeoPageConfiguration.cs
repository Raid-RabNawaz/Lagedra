using Lagedra.Modules.ContentManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ContentManagement.Infrastructure.Configurations;

public sealed class SeoPageConfiguration : IEntityTypeConfiguration<SeoPage>
{
    public void Configure(EntityTypeBuilder<SeoPage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("seo_pages");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Slug)
            .HasMaxLength(200)
            .IsRequired();
        builder.HasIndex(s => s.Slug).IsUnique();

        builder.Property(s => s.Title).HasMaxLength(300).IsRequired();
        builder.Property(s => s.MetaTitle).HasMaxLength(200);
        builder.Property(s => s.MetaDescription).HasMaxLength(500);
        builder.Property(s => s.OgImageUrl).HasMaxLength(500);
        builder.Property(s => s.CanonicalUrl).HasMaxLength(500);
        builder.Property(s => s.NoIndex);
        builder.Property(s => s.UpdatedAtUtc).IsRequired();
    }
}
