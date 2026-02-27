using System.Collections.Immutable;
using Lagedra.Modules.ContentManagement.Domain.Enums;

namespace Lagedra.Modules.ContentManagement.Application.DTOs;

public sealed record BlogPostDetailDto(
    Guid Id,
    string Slug,
    string Title,
    string Excerpt,
    string Content,
    BlogStatus Status,
    DateTime? PublishedAt,
    Guid AuthorUserId,
    IReadOnlyList<string> Tags,
    string MetaTitle,
    string MetaDescription,
    Uri? OgImageUrl,
    int ReadingTimeMinutes,
    DateTime CreatedAt);
