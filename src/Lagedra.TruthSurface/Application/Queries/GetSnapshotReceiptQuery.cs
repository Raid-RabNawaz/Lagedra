using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Security;
using Lagedra.SharedKernel.Time;
using Lagedra.TruthSurface.Domain;
using Lagedra.TruthSurface.Infrastructure.Crypto;
using Lagedra.TruthSurface.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.TruthSurface.Application.Queries;

public sealed record GetSnapshotReceiptQuery(Guid SnapshotId, Guid? RequestedByUserId) : IRequest<Result<SnapshotReceipt>>;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance", "CA1819:Properties should not return arrays",
    Justification = "Receipt body is a fixed binary payload streamed to the HTTP response.")]
public sealed record SnapshotReceipt(
    byte[] Bytes,
    string FileName,
    string ContentType,
    string Hash,
    bool IsValid);

public sealed class GetSnapshotReceiptQueryHandler(
    TruthSurfaceDbContext dbContext,
    ICryptographicSigner signer,
    IClock clock)
    : IRequestHandler<GetSnapshotReceiptQuery, Result<SnapshotReceipt>>
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<Result<SnapshotReceipt>> Handle(GetSnapshotReceiptQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = await dbContext.Snapshots
            .AsNoTracking()
            .Include(s => s.Proof)
            .FirstOrDefaultAsync(s => s.Id == request.SnapshotId, cancellationToken)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            return Result<SnapshotReceipt>.Failure(new Error("TruthSurface.NotFound", "Snapshot not found."));
        }

        if (snapshot.Status is not (TruthSurfaceStatus.Confirmed or TruthSurfaceStatus.Superseded)
            || snapshot.Proof is null
            || string.IsNullOrWhiteSpace(snapshot.CanonicalContent))
        {
            return Result<SnapshotReceipt>.Failure(
                new Error("TruthSurface.NotSealed", "Snapshot has not been sealed yet — no receipt available."));
        }

        var recomputed = CanonicalHasher.ComputeHash(snapshot.CanonicalContent);
        var hashOk = string.Equals(recomputed, snapshot.Proof.Hash, StringComparison.Ordinal);
        var sigOk = signer.Verify(Encoding.UTF8.GetBytes(snapshot.Proof.Hash), snapshot.Proof.Signature);

        // Receipt body re-parses canonical JSON so the consumer can read both
        // the human-readable structured payload and the raw signed string.
        JsonElement parsed;
        using (var doc = JsonDocument.Parse(snapshot.CanonicalContent))
        {
            parsed = doc.RootElement.Clone();
        }

        var body = new
        {
            kind = "lagedra.truth-surface.receipt.v1",
            snapshotId = snapshot.Id,
            dealId = snapshot.DealId,
            status = snapshot.Status.ToString(),
            protocolVersion = snapshot.ProtocolVersion,
            jurisdictionPackVersion = snapshot.JurisdictionPackVersion,
            createdAt = snapshot.CreatedAt,
            sealedAt = snapshot.SealedAt,
            landlordConfirmed = snapshot.LandlordConfirmed,
            tenantConfirmed = snapshot.TenantConfirmed,
            inquiryClosed = snapshot.InquiryClosed,
            supersededBySnapshotId = snapshot.SupersededBySnapshotId,
            export = new
            {
                exportedAt = clock.UtcNow,
                exportedByUserId = request.RequestedByUserId,
                verifiedAtExport = hashOk && sigOk
            },
            proof = new
            {
                hash = snapshot.Proof.Hash,
                signature = snapshot.Proof.Signature,
                signedAt = snapshot.Proof.SignedAt,
                algorithm = "SHA-256 + HMAC-SHA256",
                hashEncoding = "hex-uppercase"
            },
            canonical = new
            {
                content = parsed,
                rawJson = snapshot.CanonicalContent
            }
        };

        var json = JsonSerializer.Serialize(body, s_jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        var fileName = $"truth-surface-{snapshot.Id:N}.json";

        return Result<SnapshotReceipt>.Success(new SnapshotReceipt(
            bytes, fileName, "application/json", snapshot.Proof.Hash, hashOk && sigOk));
    }
}
