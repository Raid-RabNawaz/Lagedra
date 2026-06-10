using Lagedra.Infrastructure.Caching;
using Lagedra.Modules.JurisdictionPacks.Domain.Events;
using Lagedra.SharedKernel.Caching;
using Lagedra.SharedKernel.Events;

namespace Lagedra.Modules.JurisdictionPacks.Application.EventHandlers;

public sealed class OnPackPublishedInvalidateCacheHandler(ICacheService cache)
    : IDomainEventHandler<JurisdictionPackPublishedEvent>
{
    public async Task Handle(JurisdictionPackPublishedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var cacheKey = CacheKeys.JurisdictionPack(domainEvent.JurisdictionCode);
        await cache.RemoveAsync(cacheKey, ct).ConfigureAwait(false);
    }
}
