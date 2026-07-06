namespace Lagedra.Modules.ActivationAndBilling.Application.Services;

/// <summary>
/// Versioning + default text identifiers for the Truth Surface consent the
/// tenant gives at request time and the host gives at approval time. Bumping
/// <see cref="CurrentVersion"/> when the agreement wording changes keeps the
/// recorded consent provably tied to the exact text the party agreed to.
/// </summary>
public static class BookingConsent
{
    public const string CurrentVersion = "ts-consent-v3";

    /// <summary>Consent version recorded for implicit instant-book host approval.</summary>
    public const string InstantBookHostVersion = "ts-consent-instant-book-v1";
}
