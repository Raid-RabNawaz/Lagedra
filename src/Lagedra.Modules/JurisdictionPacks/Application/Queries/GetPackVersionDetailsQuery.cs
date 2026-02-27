using Lagedra.Modules.JurisdictionPacks.Application.DTOs;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Repositories;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.JurisdictionPacks.Application.Queries;

public sealed record GetPackVersionDetailsQuery(Guid PackId, Guid VersionId) : IRequest<Result<PackVersionDetailDto>>;

public sealed class GetPackVersionDetailsQueryHandler(JurisdictionPackRepository repository)
    : IRequestHandler<GetPackVersionDetailsQuery, Result<PackVersionDetailDto>>
{
    public async Task<Result<PackVersionDetailDto>> Handle(GetPackVersionDetailsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pack = await repository.GetByIdAsync(request.PackId, cancellationToken).ConfigureAwait(false);

        if (pack is null)
        {
            return Result<PackVersionDetailDto>.Failure(
                new Error("JurisdictionPack.NotFound", $"Pack '{request.PackId}' not found."));
        }

        var version = pack.Versions.FirstOrDefault(v => v.Id == request.VersionId);

        if (version is null)
        {
            return Result<PackVersionDetailDto>.Failure(
                new Error("PackVersion.NotFound", $"Version '{request.VersionId}' not found."));
        }

        return Result<PackVersionDetailDto>.Success(
            new PackVersionDetailDto(
                version.Id, version.PackId, version.VersionNumber, version.Status,
                version.EffectiveDate, version.ApprovedAt, version.ApprovedBy, version.SecondApproverId,
                version.EffectiveDateRules.Select(r => new EffectiveDateRuleDto(r.Id, r.FieldName, r.EffectiveDate)).ToList(),
                version.FieldGatingRules.Select(r => new FieldGatingRuleDto(r.Id, r.FieldName, r.GatingType, r.Value, r.Condition)).ToList(),
                version.EvidenceSchedules.Select(r => new EvidenceScheduleDto(r.Id, r.Category, r.MinimumRequirements)).ToList()));
    }
}
