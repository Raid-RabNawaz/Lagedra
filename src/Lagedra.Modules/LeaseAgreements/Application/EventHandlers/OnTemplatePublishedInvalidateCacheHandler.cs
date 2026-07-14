using Lagedra.Infrastructure.Caching;
using Lagedra.Modules.LeaseAgreements.Domain.Events;
using Lagedra.SharedKernel.Events;
using Microsoft.Extensions.Caching.Memory;

namespace Lagedra.Modules.LeaseAgreements.Application.EventHandlers;

public sealed class OnTemplatePublishedInvalidateCacheHandler(IMemoryCache cache)
    : IDomainEventHandler<LeaseAgreementTemplatePublishedEvent>
{
    public Task Handle(LeaseAgreementTemplatePublishedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        cache.Remove(CacheKeys.LeaseAgreementTemplate(domainEvent.JurisdictionCode));
        return Task.CompletedTask;
    }
}
