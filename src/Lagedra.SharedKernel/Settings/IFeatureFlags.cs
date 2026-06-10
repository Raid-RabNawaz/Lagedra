namespace Lagedra.SharedKernel.Settings;

/// <summary>
/// Read-only accessor for runtime feature flags. Resolves boolean toggles
/// (e.g. <c>BookingFlow.V2</c>) from environment variables or app settings.
///
/// All flags default to <c>false</c> when unset so production stays on the
/// pre-existing behaviour until ops explicitly opts in.
/// </summary>
public interface IFeatureFlags
{
    /// <summary>
    /// True when the <c>BookingFlow.V2</c> rollout is active. When enabled the
    /// guest journey collapses to inline Truth Surface confirmation, real
    /// instant booking, save-card-on-file, magic-link host approve, and the
    /// renamed <c>Checkout</c> deal phase. See PLAN.md Phase 16.
    /// </summary>
    bool BookingFlowV2Enabled { get; }

    /// <summary>
    /// Generic lookup for any other named flag. Returns <paramref name="defaultValue"/>
    /// when the flag is unset or unparseable.
    /// </summary>
    bool IsEnabled(string flagName, bool defaultValue = false);
}
