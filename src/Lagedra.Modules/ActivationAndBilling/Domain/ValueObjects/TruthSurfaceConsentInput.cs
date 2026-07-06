namespace Lagedra.Modules.ActivationAndBilling.Domain.ValueObjects;

/// <summary>
/// A party's Truth Surface consent captured at the moment they act — the tenant
/// at request time, the host at approval time. The metadata (IP, user agent,
/// consent text version) is recorded so the sealed agreement can prove who
/// agreed to what, when, and from where.
/// </summary>
public sealed record TruthSurfaceConsentInput(
    bool Given,
    string ConsentVersion,
    string? IpAddress,
    string? UserAgent);
