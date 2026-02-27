using System.Collections.Immutable;
using Lagedra.Modules.ContentManagement.Domain.Enums;
using Lagedra.Modules.ContentManagement.Domain.Events;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ContentManagement.Domain.Aggregates;

public sealed class BlogPost : AggregateRoot<Guid>
{
    public string Slug { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Excerpt { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public BlogStatus Status { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public Guid AuthorUserId { get; private set; }
    private List<string> _tags = new();
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();
    public string MetaTitle { get; private set; } = string.Empty;
    public string MetaDescription { get; private set; } = string.Empty;
    public Uri? OgImageUrl { get; private set; }
    public int ReadingTimeMinutes { get; private set; }

    private BlogPost() { }

    public static BlogPost CreateDraft(
        string slug,
        string title,
        string excerpt,
        string content,
        Guid authorUserId,
        string[] tags,
        string metaTitle,
        string metaDescription,
        Uri? ogImageUrl,
        int readingTimeMinutes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentNullException.ThrowIfNull(tags);

        return new BlogPost
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Title = title,
            Excerpt = excerpt,
            Content = content,
            Status = BlogStatus.Draft,
            AuthorUserId = authorUserId,
            _tags = tags.ToList(),
            MetaTitle = metaTitle,
            MetaDescription = metaDescription,
            OgImageUrl = ogImageUrl,
            ReadingTimeMinutes = readingTimeMinutes
        };
    }

    public void Update(
        string title,
        string excerpt,
        string content,
        string[] tags,
        string metaTitle,
        string metaDescription,
        Uri? ogImageUrl,
        int readingTimeMinutes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentNullException.ThrowIfNull(tags);

        Title = title;
        Excerpt = excerpt;
        Content = content;
        _tags = tags.ToList();
        MetaTitle = metaTitle;
        MetaDescription = metaDescription;
        OgImageUrl = ogImageUrl;
        ReadingTimeMinutes = readingTimeMinutes;
    }

    public void Publish()
    {
        if (Status == BlogStatus.Published)
        {
            throw new InvalidOperationException("Blog post is already published.");
        }

        if (Status == BlogStatus.Archived)
        {
            throw new InvalidOperationException("Cannot publish an archived blog post.");
        }

        Status = BlogStatus.Published;
        PublishedAt ??= DateTime.UtcNow;

        AddDomainEvent(new BlogPostPublishedEvent(Id, Slug, Title, PublishedAt.Value));
    }

    public void Archive()
    {
        if (Status == BlogStatus.Archived)
        {
            throw new InvalidOperationException("Blog post is already archived.");
        }

        Status = BlogStatus.Archived;

        AddDomainEvent(new BlogPostArchivedEvent(Id, Slug));
    }
}
