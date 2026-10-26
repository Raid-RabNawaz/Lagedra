using Lagedra.Infrastructure.External.Channels;
using Lagedra.Modules.ChannelIntegration.Application.DTOs;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Services;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Security;
using MediatR;

namespace Lagedra.Modules.ChannelIntegration.Application.Commands;

/// <summary>
/// Connects a host's external PMS / channel account to Lagedra using credentials
/// the host supplies directly (an API key or token pair). Provider-agnostic: the
/// chosen <paramref name="ProviderKey"/> is validated against the registry of
/// installed providers. The API secret is encrypted before it touches the DB.
///
/// A host may hold at most one connection per provider — a single API credential
/// covers the whole PMS account, so a second one would import the same properties
/// twice. To move to a different account the host disconnects the existing
/// connection first.
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
    ChannelConnectionLinker linker,
    OwnerRezOAuthFlow ownerRezOAuth,
    IChannelProviderRegistry providers,
    IEncryptionService encryption) : IRequestHandler<ConnectChannelCommand, Result<ChannelConnectionDto>>
{
    private static readonly Error UnknownProvider = new(
        "Channel.UnknownProvider",
        "No channel provider is registered for the requested key.");

    private static readonly Error InvalidAccount = new(
        "Channel.InvalidAccount",
        "An account identifier and a name for the connection are both required.");

    private static readonly Error OwnerRezRequiresOAuth = new(
        "Channel.OwnerRezRequiresOAuth",
        "OwnerRez is now connected by authorizing Lagedra in your OwnerRez account, not with an API "
        + "token. Use Connect with OwnerRez.");

    private static readonly Error OpenApiDisabled = new(
        "Channel.OpenApiDisabled",
        "OwnerRez is connected with the channel Connection ID. API keys are not accepted.");

    private static readonly Error HaXmlNotConfigured = new(
        "Channel.HaXmlNotConfigured",
        "OwnerRez channel import is not available right now. Please try again later.");

    private static readonly Error InvalidAdvertiserId = new(
        "Channel.InvalidAdvertiserId",
        "Enter the OwnerRez Connection ID from the channel page. It starts with ora.");

    public async Task<Result<ChannelConnectionDto>> Handle(
        ConnectChannelCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var provider = providers.Resolve(request.ProviderKey);
        if (provider is null)
        {
            return Result<ChannelConnectionDto>.Failure(UnknownProvider);
        }

        var accountId = request.ExternalAccountId?.Trim() ?? string.Empty;
        if (provider.ProviderKey == OwnerRezOAuthFlow.ProviderKey && !ownerRezOAuth.IsOpenApiEnabled)
        {
            // Used only when Channels:OwnerRez:OpenApiEnabled is false. The public
            // API (token or OAuth) is the connect path while that flag is true.
            if (!ownerRezOAuth.IsHaXmlConfigured)
            {
                return Result<ChannelConnectionDto>.Failure(HaXmlNotConfigured);
            }

            if (!string.IsNullOrWhiteSpace(request.Secret))
            {
                return Result<ChannelConnectionDto>.Failure(OpenApiDisabled);
            }

            if (!OwnerRezAdvertiserIds.IsValid(accountId))
            {
                return Result<ChannelConnectionDto>.Failure(InvalidAdvertiserId);
            }

            accountId = OwnerRezAdvertiserIds.Canonical(accountId);
        }
        else if (provider.ProviderKey == OwnerRezOAuthFlow.ProviderKey && ownerRezOAuth.IsConfigured)
        {
            // Once an OwnerRez OAuth app is configured it becomes the only way in:
            // personal access tokens are capped at two accounts per IP per day, so
            // letting them back in after the switch would silently reintroduce that
            // ceiling for whoever used the older path.
            return Result<ChannelConnectionDto>.Failure(OwnerRezRequiresOAuth);
        }

        if (string.IsNullOrWhiteSpace(accountId)
            || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return Result<ChannelConnectionDto>.Failure(InvalidAccount);
        }

        var encryptedSecret = string.IsNullOrWhiteSpace(request.Secret)
            ? null
            : encryption.Encrypt(request.Secret);

        // Use the registry's key rather than the caller's spelling so the
        // one-per-provider rule cannot be sidestepped by casing.
        var linked = await linker.LinkAsync(
                new LinkChannelRequest(
                    request.HostUserId,
                    provider.ProviderKey,
                    accountId,
                    request.DisplayName,
                    request.Username,
                    encryptedSecret),
                cancellationToken)
            .ConfigureAwait(false);

        if (linked.IsFailure)
        {
            return Result<ChannelConnectionDto>.Failure(linked.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ChannelConnectionDto>.Success(ChannelConnectionMapper.ToDto(linked.Value));
    }
}
