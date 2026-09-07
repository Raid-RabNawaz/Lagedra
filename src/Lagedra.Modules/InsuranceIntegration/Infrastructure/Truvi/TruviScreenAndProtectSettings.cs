namespace Lagedra.Modules.InsuranceIntegration.Infrastructure.Truvi;

public sealed class TruviScreenAndProtectSettings
{
    public const string SectionName = "Insurance:Truvi";

    public Uri BaseUrl { get; init; } = new("https://developer.api.truvi.com/screen-and-protect-sandbox");

    public string? SubscriptionKey { get; init; }

    /// <summary>
    /// Unused at request time. <c>company</c> is the listing host or
    /// property manager, not the Lagedra platform.
    /// </summary>
    public string CompanyName { get; init; } = "Lagedra";

    /// <summary>
    /// Unused at request time. See <see cref="CompanyName"/>.
    /// </summary>
    public string CompanyEmail { get; init; } = "raid@lagedra.com";

    public int ExtendedAmount { get; init; } = 50_000;

    public bool ScreeningEnabled { get; init; } = true;

    public bool CanCallApi =>
        ScreeningEnabled && !string.IsNullOrWhiteSpace(SubscriptionKey);
}
