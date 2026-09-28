using Lagedra.Modules.LeaseAgreements.Application.DTOs;
using Lagedra.Modules.LeaseAgreements.Domain.Entities;
using Lagedra.Modules.LeaseAgreements.Domain.Enums;
using Lagedra.SharedKernel.Integration;

namespace Lagedra.Modules.LeaseAgreements.Application.Services;

internal static class DealLeaseSigning
{
    public static bool IsRequired(ListingDetailsDto? listing) =>
        listing is not null
        && listing.RequireLeaseSignatures
        && (string.IsNullOrWhiteSpace(listing.LeaseAgreementSource)
            || string.Equals(listing.LeaseAgreementSource, "LagedraTemplate", StringComparison.Ordinal));

    public static DealLeaseSignerRole? RoleFor(Guid callerUserId, DealParticipantsDto participants)
    {
        if (callerUserId == participants.LandlordUserId)
        {
            return DealLeaseSignerRole.Host;
        }

        if (callerUserId == participants.TenantUserId)
        {
            return DealLeaseSignerRole.Tenant;
        }

        return null;
    }

    public static DealLeaseSigningStatusDto Status(
        bool signingRequired,
        DealLeaseSignerRole? callerRole,
        IReadOnlyList<DealLeaseSignature> signatures)
    {
        var host = signatures.FirstOrDefault(s => s.Role == DealLeaseSignerRole.Host);
        var tenant = signatures.FirstOrDefault(s => s.Role == DealLeaseSignerRole.Tenant);
        var callerHasSigned = callerRole switch
        {
            DealLeaseSignerRole.Host => host is not null,
            DealLeaseSignerRole.Tenant => tenant is not null,
            _ => false,
        };

        return new DealLeaseSigningStatusDto(
            signingRequired,
            callerRole?.ToString(),
            CallerCanSign: signingRequired && callerRole is not null && !callerHasSigned,
            CallerHasSigned: callerHasSigned,
            BothSigned: host is not null && tenant is not null,
            Host: Map(host),
            Tenant: Map(tenant));
    }

    private static DealLeasePartySignatureDto? Map(DealLeaseSignature? signature) =>
        signature is null
            ? null
            : new DealLeasePartySignatureDto(
                signature.Role.ToString(),
                signature.TypedName,
                signature.SignedAtUtc);
}
