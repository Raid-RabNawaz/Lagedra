namespace Lagedra.Modules.LeaseAgreements.Application.DTOs;

public sealed record DealLeasePartySignatureDto(
    string Role,
    string TypedName,
    DateTime SignedAtUtc);

public sealed record DealLeaseSigningStatusDto(
    bool SigningRequired,
    string? CallerRole,
    bool CallerCanSign,
    bool CallerHasSigned,
    bool BothSigned,
    DealLeasePartySignatureDto? Host,
    DealLeasePartySignatureDto? Tenant);
