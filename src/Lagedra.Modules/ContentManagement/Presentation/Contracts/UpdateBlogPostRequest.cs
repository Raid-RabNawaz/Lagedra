namespace Lagedra.Modules.ContentManagement.Presentation.Contracts;

public sealed record UpdateBlogPostRequest(
    string Title,
    string Excerpt,
    string Content,
    IReadOnlyList<string> Tags,
    string MetaTitle,
    string MetaDescription,
    Uri? OgImageUrl,
    int ReadingTimeMinutes);
