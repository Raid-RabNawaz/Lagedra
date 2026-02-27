using Lagedra.Modules.JurisdictionPacks.Application.DTOs;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.JurisdictionPacks.Application.Queries;

public sealed record GetActivePackForJurisdictionQuery(string JurisdictionCode) : IRequest<Result<JurisdictionPackDto>>;

public sealed class GetActivePackForJurisdictionQueryHandler(JurisdictionDbContext dbContext)
    : IRequestHandler<GetActivePackForJurisdictionQuery, Result<JurisdictionPackDto>>
{
    public async Task<Result<JurisdictionPackDto>> Handle(GetActivePackForJurisdictionQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var code = request.JurisdictionCode.ToUpperInvariant();

        var pack = await dbContext.JurisdictionPacks
            .AsNoTracking()
            .Include(p => p.Versions)
            .FirstOrDefaultAsync(p => p.JurisdictionCode.Code == code, cancellationToken)
            .ConfigureAwait(false);

        if (pack is null)
        {
            return Result<JurisdictionPackDto>.Failure(
                new Error("JurisdictionPack.NotFound", $"No pack found for jurisdiction '{request.JurisdictionCode}'."));
        }

        return Result<JurisdictionPackDto>.Success(
            new JurisdictionPackDto(
                pack.Id,
                pack.JurisdictionCode.Code,
                pack.ActiveVersionId,
                pack.Versions.Select(v => new PackVersionSummaryDto(
                    v.Id, v.VersionNumber, v.Status,
                    v.EffectiveDate, v.ApprovedAt,
                    v.ApprovedBy, v.SecondApproverId)).ToList()));
    }
}
