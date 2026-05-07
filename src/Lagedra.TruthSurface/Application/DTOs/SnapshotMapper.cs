using System.Text;
using Lagedra.SharedKernel.Security;
using Lagedra.TruthSurface.Domain;
using Lagedra.TruthSurface.Infrastructure.Crypto;

namespace Lagedra.TruthSurface.Application.DTOs;

/// <summary>
/// Centralised mapping from a <see cref="TruthSnapshot"/> to a wire DTO.
/// Importantly, when a proof is present, this mapper actually re-verifies the
/// SHA-256 hash and HMAC signature against the persisted canonical content.
/// Reads no longer report <c>isValid = true</c> blindly — if storage was tampered
/// with, callers will see <c>isValid = false</c>.
/// </summary>
internal static class SnapshotMapper
{
    public static TruthSurfaceDto Map(TruthSnapshot s, ICryptographicSigner signer)
    {
        ArgumentNullException.ThrowIfNull(s);
        ArgumentNullException.ThrowIfNull(signer);

        SnapshotProofDto? proof = null;

        if (s.Proof is not null && !string.IsNullOrWhiteSpace(s.CanonicalContent))
        {
            var recomputed = CanonicalHasher.ComputeHash(s.CanonicalContent);
            var hashOk = string.Equals(recomputed, s.Proof.Hash, StringComparison.Ordinal);
            var sigOk = signer.Verify(Encoding.UTF8.GetBytes(s.Proof.Hash), s.Proof.Signature);

            proof = new SnapshotProofDto(
                s.Proof.Id,
                s.Proof.Hash,
                s.Proof.Signature,
                s.Proof.SignedAt,
                IsValid: hashOk && sigOk);
        }

        return new TruthSurfaceDto(
            s.Id,
            s.DealId,
            s.Status,
            s.ProtocolVersion,
            s.JurisdictionPackVersion,
            s.CanonicalContent,
            s.InquiryClosed,
            s.LandlordConfirmed,
            s.TenantConfirmed,
            s.CreatedAt,
            s.SealedAt,
            proof);
    }
}
