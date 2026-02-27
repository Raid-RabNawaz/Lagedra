using Lagedra.Modules.ContentManagement.Application.DTOs;
using Lagedra.Modules.ContentManagement.Domain.Entities;
using Lagedra.Modules.ContentManagement.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ContentManagement.Application.Commands;

public sealed record UpsertSeoPageCommand(
    string Slug,
    string Title,
    string MetaTitle,
    string MetaDescription,
    Uri? OgImageUrl,
    Uri? CanonicalUrl,
    bool NoIndex) : IRequest<Result<SeoPageDto>>;

public sealed class UpsertSeoPageCommandHandler(
    ContentDbContext dbContext)
    : IRequestHandler<UpsertSeoPageCommand, Result<SeoPageDto>>
{
    public async Task<Result<SeoPageDto>> Handle(
        UpsertSeoPageCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await dbContext.SeoPages
            .FirstOrDefaultAsync(p => p.Slug == request.Slug, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            existing.Update(
                request.Title,
                request.MetaTitle,
                request.MetaDescription,
                request.OgImageUrl,
                request.CanonicalUrl,
                request.NoIndex);
        }
        else
        {
            existing = SeoPage.Create(
                request.Slug,
                request.Title,
                request.MetaTitle,
                request.MetaDescription,
                request.OgImageUrl,
                request.CanonicalUrl,
                request.NoIndex);

            dbContext.SeoPages.Add(existing);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<SeoPageDto>.Success(
            new SeoPageDto(
                existing.Id, existing.Slug, existing.Title, existing.MetaTitle,
                existing.MetaDescription, existing.OgImageUrl, existing.CanonicalUrl,
                existing.NoIndex, existing.UpdatedAtUtc));
    }
}
