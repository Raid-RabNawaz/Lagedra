using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record ApproveDamageClaimCommand(
    Guid DealId,
    Guid ClaimId,
    long ApprovedAmountCents,
    string? Notes) : IRequest<Result<DamageClaimDto>>;

public sealed class ApproveDamageClaimCommandHandler(BillingDbContext dbContext)
    : IRequestHandler<ApproveDamageClaimCommand, Result<DamageClaimDto>>
{
    public async Task<Result<DamageClaimDto>> Handle(
        ApproveDamageClaimCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var claim = await dbContext.DamageClaims
            .FirstOrDefaultAsync(c => c.Id == request.ClaimId && c.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (claim is null)
        {
            return Result<DamageClaimDto>.Failure(
                new Error("DamageClaim.NotFound", "Damage claim not found."));
        }

        claim.Approve(request.ApprovedAmountCents, request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<DamageClaimDto>.Success(MapToDto(claim));
    }

    private static DamageClaimDto MapToDto(Domain.Aggregates.DamageClaim c) =>
        new(c.Id, c.DealId, c.ListingId, c.FiledByUserId, c.TenantUserId,
            c.Status, c.Description, c.ClaimedAmountCents, c.ApprovedAmountCents,
            c.DepositDeductionCents, c.InsuranceClaimCents, c.EvidenceManifestId,
            c.FiledAt, c.ResolvedAt, c.ResolutionNotes);
}
