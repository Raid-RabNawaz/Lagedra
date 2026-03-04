using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ContentManagement.Domain.Entities;

public sealed class SeoPage : Entity<Guid>
{
    public string Slug { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string MetaTitle { get; private set; } = string.Empty;
    public string MetaDescription { get; private set; } = string.Empty;
    public Uri? OgImageUrl { get; private set; }
    public Uri? CanonicalUrl { get; private set; }
    public bool NoIndex { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private SeoPage() { }

    public static SeoPage Create(
        string slug,
        string title,
        string metaTitle,
        string metaDescription,
        Uri? ogImageUrl,
        Uri? canonicalUrl,
        bool noIndex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        return new SeoPage
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Title = title,
            MetaTitle = metaTitle,
            MetaDescription = metaDescription,
            OgImageUrl = ogImageUrl,
            CanonicalUrl = canonicalUrl,
            NoIndex = noIndex,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        string title,
        string metaTitle,
        string metaDescription,
        Uri? ogImageUrl,
        Uri? canonicalUrl,
        bool noIndex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title;
        MetaTitle = metaTitle;
        MetaDescription = metaDescription;
        OgImageUrl = ogImageUrl;
        CanonicalUrl = canonicalUrl;
        NoIndex = noIndex;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
