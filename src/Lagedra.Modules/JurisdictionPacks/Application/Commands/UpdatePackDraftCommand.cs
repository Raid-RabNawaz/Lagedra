using FluentValidation;
using Lagedra.Modules.JurisdictionPacks.Application.DTOs;
using Lagedra.Modules.JurisdictionPacks.Domain.Enums;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Repositories;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.JurisdictionPacks.Application.Commands;

public sealed record UpdatePackDraftCommand(
    Guid PackId,
    Guid VersionId,
    DateTime? EffectiveDate,
    IReadOnlyList<EffectiveDateRuleInput>? EffectiveDateRules,
    IReadOnlyList<FieldGatingRuleInput>? FieldGatingRules,
    IReadOnlyList<EvidenceScheduleInput>? EvidenceSchedules,
    IReadOnlyList<DepositCapRuleInput>? DepositCapRules) : IRequest<Result<PackVersionDetailDto>>;

public sealed record EffectiveDateRuleInput(string FieldName, DateTime EffectiveDate);
public sealed record FieldGatingRuleInput(string FieldName, GatingType GatingType, string Value, string? Condition);
public sealed record EvidenceScheduleInput(string Category, string MinimumRequirements);
public sealed record DepositCapRuleInput(
    string JurisdictionCode, decimal MaxMultiplier, string LegalReference,
    string? ExceptionCondition = null, decimal? ExceptionMultiplier = null);

public sealed class UpdatePackDraftCommandValidator : AbstractValidator<UpdatePackDraftCommand>
{
    public UpdatePackDraftCommandValidator()
    {
        RuleFor(x => x.PackId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
    }
}

public sealed class UpdatePackDraftCommandHandler(JurisdictionPackRepository repository)
    : IRequestHandler<UpdatePackDraftCommand, Result<PackVersionDetailDto>>
{
    public async Task<Result<PackVersionDetailDto>> Handle(UpdatePackDraftCommand request, CancellationToken cancellationToken)
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

        if (request.EffectiveDate.HasValue)
        {
            version.SetEffectiveDate(request.EffectiveDate.Value);
        }

        if (request.EffectiveDateRules is not null)
        {
            foreach (var rule in request.EffectiveDateRules)
            {
                version.AddEffectiveDateRule(rule.FieldName, rule.EffectiveDate);
            }
        }

        if (request.FieldGatingRules is not null)
        {
            foreach (var rule in request.FieldGatingRules)
            {
                version.AddFieldGatingRule(rule.FieldName, rule.GatingType, rule.Value, rule.Condition);
            }
        }

        if (request.EvidenceSchedules is not null)
        {
            foreach (var schedule in request.EvidenceSchedules)
            {
                version.AddEvidenceSchedule(schedule.Category, schedule.MinimumRequirements);
            }
        }

        if (request.DepositCapRules is not null)
        {
            foreach (var rule in request.DepositCapRules)
            {
                version.AddDepositCapRule(
                    rule.JurisdictionCode, rule.MaxMultiplier, rule.LegalReference,
                    rule.ExceptionCondition, rule.ExceptionMultiplier);
            }
        }

        repository.Update(pack);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PackVersionDetailDto>.Success(MapToDto(version));
    }

    private static PackVersionDetailDto MapToDto(Domain.Entities.PackVersion v) =>
        new(v.Id, v.PackId, v.VersionNumber, v.Status,
            v.EffectiveDate, v.ApprovedAt, v.ApprovedBy, v.SecondApproverId,
            v.EffectiveDateRules.Select(r => new EffectiveDateRuleDto(r.Id, r.FieldName, r.EffectiveDate)).ToList(),
            v.FieldGatingRules.Select(r => new FieldGatingRuleDto(r.Id, r.FieldName, r.GatingType, r.Value, r.Condition)).ToList(),
            v.EvidenceSchedules.Select(r => new EvidenceScheduleDto(r.Id, r.Category, r.MinimumRequirements)).ToList(),
            v.DepositCapRules.Select(r => new DepositCapRuleDto(r.Id, r.JurisdictionCode, r.MaxMultiplier, r.ExceptionCondition, r.ExceptionMultiplier, r.LegalReference)).ToList());
}
