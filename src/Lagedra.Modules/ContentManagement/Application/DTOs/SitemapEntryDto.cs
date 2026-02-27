namespace Lagedra.Modules.ContentManagement.Application.DTOs;

public sealed record SitemapEntryDto(
    string Slug,
    DateTime LastModified,
    string ChangeFrequency,
    double Priority);
