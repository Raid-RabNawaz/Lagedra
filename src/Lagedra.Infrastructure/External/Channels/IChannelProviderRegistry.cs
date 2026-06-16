namespace Lagedra.Infrastructure.External.Channels;

/// <summary>
/// Resolves a concrete <see cref="IChannelProvider"/> by its provider key.
/// Adding a new PMS integration is purely additive: register another
/// <see cref="IChannelProvider"/> in DI and it becomes resolvable here — no
/// changes to the calling code (sync jobs, booking publisher, endpoints).
/// </summary>
public interface IChannelProviderRegistry
{
    IReadOnlyCollection<IChannelProvider> All { get; }

    IChannelProvider? Resolve(string providerKey);
}
