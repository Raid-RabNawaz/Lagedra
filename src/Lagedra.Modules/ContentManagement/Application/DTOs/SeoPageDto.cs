namespace Lagedra.Modules.ContentManagement.Application.DTOs;

public sealed record SeoPageDto(
    Guid Id,
    string Slug,
    string Title,
    string MetaTitle,
    string MetaDescription,
    Uri? OgImageUrl,
    Uri? CanonicalUrl,
    bool NoIndex,
    DateTime UpdatedAtUtc);
