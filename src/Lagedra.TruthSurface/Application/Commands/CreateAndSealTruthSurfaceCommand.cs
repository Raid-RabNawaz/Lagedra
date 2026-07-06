using System.Text;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Security;
using Lagedra.SharedKernel.Time;
using Lagedra.TruthSurface.Application.DTOs;
using Lagedra.TruthSurface.Application.Services;
using Lagedra.TruthSurface.Infrastructure.Crypto;
using Lagedra.TruthSurface.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.TruthSurface.Application.Commands;

/// <summary>
/// Atomically creates the Truth Surface for an approved deal, records BOTH
/// parties' consent (tenant captured at request time, host at approval time),
/// and seals it in a single step — the new predetermined-deposit host-approval
/// path. Raises <c>TruthSurfaceConfirmedEvent</c> on seal, which drives the
/// off-session charge + activation.
/// </summary>
public sealed record CreateAndSealTruthSurfaceCommand(
    Guid DealId,
    Guid TenantUserId,
    DateTime TenantConsentAt,
    string? TenantConsentIp,
    string? TenantConsentUserAgent,
    string TenantConsentVersion,
    Guid HostUserId,
    DateTime HostConsentAt,
    string? HostConsentIp,
    string? HostConsentUserAgent,
    string HostConsentVersion) : IRequest<Result<TruthSurfaceDto>>;

public sealed class CreateAndSealTruthSurfaceCommandHandler(
    TruthSurfaceDbContext dbContext,
    ITruthSurfaceSnapshotBuilder snapshotBuilder,
    ICryptographicSigner signer,
    IClock clock)
    : IRequestHandler<CreateAndSealTruthSurfaceCommand, Result<TruthSurfaceDto>>
{
    public async Task<Result<TruthSurfaceDto>> Handle(
        CreateAndSealTruthSurfaceCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var consent = new SnapshotConsentInput(
            request.TenantUserId,
            request.TenantConsentAt,
            request.TenantConsentIp,
            request.TenantConsentUserAgent,
            request.TenantConsentVersion,
            request.HostUserId,
            request.HostConsentAt,
            request.HostConsentIp,
            request.HostConsentUserAgent,
            request.HostConsentVersion);

        var buildResult = await snapshotBuilder
            .BuildDraftAsync(request.DealId, request.HostUserId, consent, cancellationToken)
            .ConfigureAwait(false);

        if (!buildResult.IsSuccess)
        {
            return Result<TruthSurfaceDto>.Failure(buildResult.Error);
        }

        var snapshot = buildResult.Value;

        try
        {
            snapshot.RecordBothConsents(
                request.TenantUserId,
                request.TenantConsentAt,
                request.TenantConsentIp,
                request.TenantConsentUserAgent,
                request.TenantConsentVersion,
                request.HostUserId,
                request.HostConsentAt,
                request.HostConsentIp,
                request.HostConsentUserAgent,
                request.HostConsentVersion);

            if (string.IsNullOrWhiteSpace(snapshot.CanonicalContent))
            {
                return Result<TruthSurfaceDto>.Failure(
                    new Error("TruthSurface.NoContent", "Cannot seal: snapshot has no canonical content."));
            }

            var hash = CanonicalHasher.ComputeHash(snapshot.CanonicalContent);
            var signature = signer.Sign(Encoding.UTF8.GetBytes(hash));
            snapshot.Seal(hash, signature, clock.UtcNow);

            dbContext.Snapshots.Add(snapshot);

            // CryptographicProof gets a fresh Guid id; mark it Added so EF Core
            // INSERTs it instead of issuing an UPDATE for a "non-default key".
            if (snapshot.Proof is not null)
            {
                dbContext.Entry(snapshot.Proof).State = EntityState.Added;
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Result<TruthSurfaceDto>.Success(SnapshotMapper.Map(snapshot, signer));
        }
        catch (InvalidOperationException ex)
        {
            return Result<TruthSurfaceDto>.Failure(
                new Error("TruthSurface.InvalidOperation", ex.Message));
        }
    }
}
