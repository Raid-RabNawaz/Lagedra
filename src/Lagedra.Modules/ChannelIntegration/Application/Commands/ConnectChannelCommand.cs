using Lagedra.Infrastructure.External.Channels;
using Lagedra.Modules.ChannelIntegration.Application.DTOs;
using Lagedra.Modules.ChannelIntegration.Domain.Aggregates;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Security;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ChannelIntegration.Application.Commands;

/// <summary>
/// Connects a host's external PMS / channel account to Lagedra. Provider-agnostic:
/// the chosen <paramref name="ProviderKey"/> is validated against the registry of
/// installed providers. The API secret is encrypted before it touches the DB.
/// </summary>
public sealed record ConnectChannelCommand(
    Guid HostUserId,
    string ProviderKey,
    string ExternalAccountId,
    string DisplayName,
    string? Username,
    string? Secret) : IRequest<Result<ChannelConnectionDto>>;

public sealed class ConnectChannelCommandHandler(
    ChannelDbContext dbContext,
    IChannelProviderRegistry providers,
    IEncryptionService encryption,
    IClock clock) : IRequestHandler<ConnectChannelCommand, Result<ChannelConnectionDto>>
{
    private static readonly Error UnknownProvider = new(
        "Channel.UnknownProvider",
        "No channel provider is registered for the requested key.");

    private static readonly Error AlreadyConnected = new(
        "Channel.AlreadyConnected",
        "This provider account is already connected for this host.");

    /// <summary>
    /// Providers where a single link covers the whole account, so a second
    /// connection would import the same properties twice.
    /// </summary>
    private static readonly Dictionary<string, (string CanonicalKey, Error Error)> SingleConnectionProviders =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["hostaway"] = ("hostaway", new(
                "Channel.HostawayAlreadyConnected",
                "You already have a Hostaway connection. Sync that connection to update listings.")),
            ["guesty"] = ("guesty", new(
                "Channel.GuestyAlreadyConnected",
                "You already have a Guesty connection. Sync that connection to update listings.")),
            ["ownerrez"] = ("ownerrez", new(
                "Channel.OwnerRezAlreadyConnected",
                "You already have an OwnerRez connection. Sync that connection to update listings.")),
        };

    public async Task<Result<ChannelConnectionDto>> Handle(
        ConnectChannelCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (providers.Resolve(request.ProviderKey) is null)
        {
            return Result<ChannelConnectionDto>.Failure(UnknownProvider);
        }

        var providerKey = request.ProviderKey.Trim();
        var externalAccountId = request.ExternalAccountId.Trim();

        if (SingleConnectionProviders.TryGetValue(providerKey, out var singleConnection))
        {
            var canonicalKey = singleConnection.CanonicalKey;
            var linked = await dbContext.Connections
                .AnyAsync(c => c.HostUserId == request.HostUserId
                            && c.ProviderKey == canonicalKey, cancellationToken)
                .ConfigureAwait(false);
            if (linked)
            {
                return Result<ChannelConnectionDto>.Failure(singleConnection.Error);
            }
        }

        var exists = await dbContext.Connections
            .AnyAsync(c => c.HostUserId == request.HostUserId
                        && c.ProviderKey == providerKey
                        && c.ExternalAccountId == externalAccountId, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            return Result<ChannelConnectionDto>.Failure(AlreadyConnected);
        }

        var encryptedSecret = string.IsNullOrWhiteSpace(request.Secret)
            ? null
            : encryption.Encrypt(request.Secret);

        var connection = ChannelConnection.Create(
            request.HostUserId,
            request.ProviderKey,
            request.ExternalAccountId,
            request.DisplayName,
            request.Username,
            encryptedSecret,
            clock);

        dbContext.Connections.Add(connection);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ChannelConnectionDto>.Success(ChannelConnectionMapper.ToDto(connection));
    }
}
