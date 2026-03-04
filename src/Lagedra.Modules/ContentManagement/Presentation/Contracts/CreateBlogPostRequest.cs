namespace Lagedra.Modules.ContentManagement.Presentation.Contracts;

public sealed record CreateBlogPostRequest(
    string Slug,
    string Title,
    string Excerpt,
    string Content,
    Guid AuthorUserId,
    IReadOnlyList<string> Tags,
    string MetaTitle,
    string MetaDescription,
    Uri? OgImageUrl,
    int ReadingTimeMinutes);
