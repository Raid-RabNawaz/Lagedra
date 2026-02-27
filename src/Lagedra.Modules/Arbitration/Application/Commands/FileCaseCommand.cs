using Lagedra.Modules.Arbitration.Application.DTOs;
using Lagedra.Modules.Arbitration.Domain.Aggregates;
using Lagedra.Modules.Arbitration.Domain.Enums;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Settings;
using MediatR;

namespace Lagedra.Modules.Arbitration.Application.Commands;

public sealed record FileCaseCommand(
    Guid DealId,
    Guid FiledByUserId,
    ArbitrationTier Tier,
    ArbitrationCategory Category) : IRequest<Result<CaseDto>>;

public sealed class FileCaseCommandHandler(
    ArbitrationDbContext dbContext,
    IPlatformSettingsService settings)
    : IRequestHandler<FileCaseCommand, Result<CaseDto>>
{
    public async Task<Result<CaseDto>> Handle(FileCaseCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var filingFee = await GetFilingFeeAsync(request.Tier, cancellationToken).ConfigureAwait(false);

        var arbitrationCase = ArbitrationCase.File(
            request.DealId, request.FiledByUserId,
            request.Tier, request.Category, filingFee);

        dbContext.ArbitrationCases.Add(arbitrationCase);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<CaseDto>.Success(MapToDto(arbitrationCase));
    }

    private async Task<long> GetFilingFeeAsync(ArbitrationTier tier, CancellationToken ct)
    {
        return tier switch
        {
            ArbitrationTier.ProtocolAdjudication => await settings.GetLongAsync(
                PlatformSettingKeys.ArbitrationFeeProtocolAdjudication, 4900, ct).ConfigureAwait(false),
            ArbitrationTier.BindingArbitration => await settings.GetLongAsync(
                PlatformSettingKeys.ArbitrationFeeBindingArbitration, 9900, ct).ConfigureAwait(false),
            _ => await settings.GetLongAsync(
                PlatformSettingKeys.ArbitrationFeeProtocolAdjudication, 4900, ct).ConfigureAwait(false)
        };
    }

    private static CaseDto MapToDto(ArbitrationCase c) =>
        new(c.Id, c.DealId, c.FiledByUserId, c.Tier, c.Category, c.Status,
            c.FilingFeeCents, c.FiledAt, c.EvidenceCompleteAt, c.DecisionDueAt,
            c.EvidenceSlots.Count, null);
}
