namespace Lagedra.Modules.ChannelIntegration.Domain.Enums;

/// <summary>
/// How the host said the PMS listing amount should be read.
/// Null on the listing map means they have not chosen yet.
/// </summary>
public enum ChannelPmsPriceBasis
{
    /// <summary>The PMS amount is a nightly rate. Monthly rent is that amount × 30.</summary>
    Nightly,

    /// <summary>The PMS amount is already the monthly rent and must not be multiplied.</summary>
    Monthly,
}
