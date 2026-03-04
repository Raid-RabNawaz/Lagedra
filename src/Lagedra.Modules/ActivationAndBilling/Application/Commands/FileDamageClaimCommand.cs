using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Settings;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record FileDamageClaimCommand(
    Guid DealId,
    Guid FiledByUserId,
    string Description,
    long ClaimedAmountCents,
    Guid? EvidenceManifestId) : IRequest<Result<DamageClaimDto>>;

public sealed class FileDamageClaimCommandHandler(
    BillingDbContext dbContext,
    IClock clock,
    IPlatformSettingsService settings)
    : IRequestHandler<FileDamageClaimCommand, Result<DamageClaimDto>>
{
    public async Task<Result<DamageClaimDto>> Handle(
        FileDamageClaimCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var application = await dbContext.DealApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (application is null)
        {
            return Result<DamageClaimDto>.Failure(
                new Error("DamageClaim.DealNotFound", "Deal not found."));
        }

        var deadlineDays = (int)await settings
            .GetLongAsync(PlatformSettingKeys.DamageClaimFilingDeadlineDays, 14, cancellationToken)
            .ConfigureAwait(false);

        var today = DateOnly.FromDateTime(clock.UtcNow);
        var daysSinceCheckout = today.DayNumber - application.RequestedCheckOut.DayNumber;

        if (daysSinceCheckout > deadlineDays)
        {
            return Result<DamageClaimDto>.Failure(
                new Error("DamageClaim.PastDeadline",
                    $"Damage claims must be filed within {deadlineDays} days of check-out."));
        }

        var existingClaim = await dbContext.DamageClaims
            .AnyAsync(c => c.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (existingClaim)
        {
            return Result<DamageClaimDto>.Failure(
                new Error("DamageClaim.AlreadyFiled", "A damage claim already exists for this deal."));
        }

        var claim = DamageClaim.File(
            request.DealId,
            application.ListingId,
            request.FiledByUserId,
            application.TenantUserId,
            request.Description,
            request.ClaimedAmountCents,
            application.DepositAmountCents ?? 0,
            request.EvidenceManifestId);

        dbContext.DamageClaims.Add(claim);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<DamageClaimDto>.Success(MapToDto(claim));
    }

    private static DamageClaimDto MapToDto(DamageClaim c) =>
        new(c.Id, c.DealId, c.ListingId, c.FiledByUserId, c.TenantUserId,
            c.Status, c.Description, c.ClaimedAmountCents, c.ApprovedAmountCents,
            c.DepositDeductionCents, c.InsuranceClaimCents, c.EvidenceManifestId,
            c.FiledAt, c.ResolvedAt, c.ResolutionNotes);
}
