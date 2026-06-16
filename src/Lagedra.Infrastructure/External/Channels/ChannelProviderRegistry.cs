namespace Lagedra.Infrastructure.External.Channels;

/// <summary>
/// Default registry that indexes every registered <see cref="IChannelProvider"/>
/// by its <see cref="IChannelProvider.ProviderKey"/> (case-insensitive).
/// </summary>
public sealed class ChannelProviderRegistry : IChannelProviderRegistry
{
    private readonly IReadOnlyCollection<IChannelProvider> _all;
    private readonly Dictionary<string, IChannelProvider> _byKey;

    public ChannelProviderRegistry(IEnumerable<IChannelProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        _all = providers.ToList();
        _byKey = _all.ToDictionary(p => p.ProviderKey, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<IChannelProvider> All => _all;

    public IChannelProvider? Resolve(string providerKey) =>
        !string.IsNullOrWhiteSpace(providerKey) && _byKey.TryGetValue(providerKey, out var provider)
            ? provider
            : null;
}
