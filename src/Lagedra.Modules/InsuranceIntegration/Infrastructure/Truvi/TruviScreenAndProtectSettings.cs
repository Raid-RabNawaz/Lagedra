namespace Lagedra.Modules.InsuranceIntegration.Infrastructure.Truvi;

public sealed class TruviScreenAndProtectSettings
{
    public const string SectionName = "Insurance:Truvi";

    /// <summary>Sandbox host. Staging and local default.</summary>
    public const string SandboxBaseUrl = "https://developer.api.truvi.com/screen-and-protect-sandbox";

    /// <summary>
    /// Production host confirmed by Truvi. Create is
    /// <c>POST {ProductionBaseUrl}/verificationRequests</c>;
    /// cancel is <c>PUT {ProductionBaseUrl}/verificationRequests/cancel</c>.
    /// Modify stays <c>PUT {ProductionBaseUrl}/verificationRequests</c>.
    /// </summary>
    public const string ProductionBaseUrl = "https://developer.api.truvi.com/screen-and-protect";

    public Uri BaseUrl { get; init; } = new(SandboxBaseUrl);

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
