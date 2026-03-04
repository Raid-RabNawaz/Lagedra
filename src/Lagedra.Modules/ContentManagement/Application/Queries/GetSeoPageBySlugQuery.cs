using Lagedra.Modules.ContentManagement.Application.DTOs;
using Lagedra.Modules.ContentManagement.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ContentManagement.Application.Queries;

public sealed record GetSeoPageBySlugQuery(string Slug) : IRequest<Result<SeoPageDto>>;

public sealed class GetSeoPageBySlugQueryHandler(ContentDbContext dbContext)
    : IRequestHandler<GetSeoPageBySlugQuery, Result<SeoPageDto>>
{
    public async Task<Result<SeoPageDto>> Handle(
        GetSeoPageBySlugQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var page = await dbContext.SeoPages
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == request.Slug, cancellationToken)
            .ConfigureAwait(false);

        if (page is null)
        {
            return Result<SeoPageDto>.Failure(
                new Error("SeoPage.NotFound", "SEO page not found."));
        }

        return Result<SeoPageDto>.Success(
            new SeoPageDto(
                page.Id, page.Slug, page.Title, page.MetaTitle,
                page.MetaDescription, page.OgImageUrl, page.CanonicalUrl,
                page.NoIndex, page.UpdatedAtUtc));
    }
}
