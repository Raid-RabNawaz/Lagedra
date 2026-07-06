using Lagedra.SharedKernel.Settings;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Infrastructure.Settings;

/// <summary>
/// Configuration-backed implementation of <see cref="IFeatureFlags"/>.
/// Flags are read from <c>FeatureFlags:&lt;flag&gt;</c> in
/// <see cref="IConfiguration"/>, which transparently picks up the
/// <c>FeatureFlags__&lt;flag&gt;</c> environment variable convention used in
/// container deployments.
/// </summary>
public sealed class FeatureFlags(IConfiguration configuration) : IFeatureFlags
{
    private const string SectionName = "FeatureFlags";

    // Defaults to enabled: the predetermined-deposit booking flow has replaced
    // the legacy host-keys-in-deposit flow in place, and the whole apply/approve
    // UI now depends on it (card + consent captured up front, no deposit input on
    // approve). Set FeatureFlags:BookingFlow.V2=false only to fall back to the
    // legacy path deliberately.
    public bool BookingFlowV2Enabled => IsEnabled("BookingFlow.V2", defaultValue: true);

    public bool IsEnabled(string flagName, bool defaultValue = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(flagName);

        var raw = configuration[$"{SectionName}:{flagName}"];

        if (string.IsNullOrWhiteSpace(raw))
        {
            return defaultValue;
        }

        return bool.TryParse(raw, out var parsed) ? parsed : defaultValue;
    }
}
