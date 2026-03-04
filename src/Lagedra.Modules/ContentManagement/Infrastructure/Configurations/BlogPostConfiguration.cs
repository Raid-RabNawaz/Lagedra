using Lagedra.Modules.ContentManagement.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ContentManagement.Infrastructure.Configurations;

public sealed class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("blog_posts");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Slug)
            .HasMaxLength(200)
            .IsRequired();
        builder.HasIndex(b => b.Slug).IsUnique();

        builder.Property(b => b.Title).HasMaxLength(300).IsRequired();
        builder.Property(b => b.Excerpt).HasMaxLength(500);
        builder.Property(b => b.Content).IsRequired();

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(b => b.AuthorUserId).IsRequired();
        builder.HasIndex(b => b.AuthorUserId);

        builder.Property(b => b.Tags)
            .HasColumnType("text[]");

        builder.Property(b => b.MetaTitle).HasMaxLength(200);
        builder.Property(b => b.MetaDescription).HasMaxLength(500);
        builder.Property(b => b.OgImageUrl).HasMaxLength(500);
        builder.Property(b => b.ReadingTimeMinutes);

        builder.Ignore(b => b.DomainEvents);
    }
}
