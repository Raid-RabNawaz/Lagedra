using Lagedra.Modules.IdentityAndVerification.Domain.Events;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.IdentityAndVerification.Application.EventHandlers;

/// <summary>
/// Syncs the IsGovernmentIdVerified flag on the Auth user record
/// whenever identity verification completes successfully.
/// </summary>
public sealed partial class OnIdentityVerifiedSyncAuthHandler(
    IUserVerificationFlagUpdater flagUpdater,
    ILogger<OnIdentityVerifiedSyncAuthHandler> logger)
    : IDomainEventHandler<IdentityVerifiedEvent>
{
    public async Task Handle(IdentityVerifiedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        LogSyncing(logger, domainEvent.UserId);

        await flagUpdater
            .MarkGovernmentIdVerifiedAsync(domainEvent.UserId, ct)
            .ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Syncing IsGovernmentIdVerified flag for user {UserId}")]
    private static partial void LogSyncing(ILogger logger, Guid userId);
}
