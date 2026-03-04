using Lagedra.Modules.ContentManagement.Domain.Enums;

namespace Lagedra.Modules.ContentManagement.Application.DTOs;

public sealed record BlogPostSummaryDto(
    Guid Id,
    string Slug,
    string Title,
    string Excerpt,
    BlogStatus Status,
    DateTime? PublishedAt,
    Guid AuthorUserId,
    IReadOnlyList<string> Tags,
    Uri? OgImageUrl,
    int ReadingTimeMinutes);
