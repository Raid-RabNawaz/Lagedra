using Lagedra.Infrastructure.External.Channels.OwnerRez;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Services;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Security;
using MediatR;

namespace Lagedra.Modules.ChannelIntegration.Application.Commands;

/// <summary>
/// Finishes the OwnerRez OAuth flow: verifies the state Lagedra minted when the
/// host set out, trades the one-time code for an access token, and links the
/// account. The access and refresh tokens are encrypted before they reach the DB.
/// </summary>
public sealed record CompleteOwnerRezOAuthCommand(
    string? Code,
    string? State) : IRequest<Result<Guid>>;

public sealed class CompleteOwnerRezOAuthCommandHandler(
    ChannelDbContext dbContext,
    ChannelConnectionLinker linker,
    OwnerRezOAuthFlow flow,
    OwnerRezOAuthClient oauthClient,
    IEncryptionService encryption) : IRequestHandler<CompleteOwnerRezOAuthCommand, Result<Guid>>
{
    private static readonly Error InvalidState = new(
        "Channel.OAuthStateInvalid",
        "That OwnerRez authorization link is no longer valid. Please start again.");

    private static readonly Error ExchangeFailed = new(
        "Channel.OAuthExchangeFailed",
        "OwnerRez would not confirm the authorization. Please try connecting again.");

    public async Task<Result<Guid>> Handle(
        CompleteOwnerRezOAuthCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (flow.TryReadState(request.State) is not { } hostUserId
            || string.IsNullOrWhiteSpace(request.Code))
        {
            return Result<Guid>.Failure(InvalidState);
        }

        var tokens = await oauthClient
            .ExchangeCodeAsync(request.Code, flow.RedirectUri, cancellationToken)
            .ConfigureAwait(false);

        if (tokens is null || string.IsNullOrWhiteSpace(tokens.UserId))
        {
            return Result<Guid>.Failure(ExchangeFailed);
        }

        var linked = await linker.LinkAsync(
                new LinkChannelRequest(
                    hostUserId,
                    OwnerRezOAuthFlow.ProviderKey,
                    tokens.UserId,
                    // OwnerRez only returns a display name on some grants; the
                    // account id keeps the card labelled either way.
                    string.IsNullOrWhiteSpace(tokens.UserDisplayName)
                        ? $"OwnerRez account {tokens.UserId}"
                        : tokens.UserDisplayName,
                    Tokens: new ChannelOAuthTokens(
                        encryption.Encrypt(tokens.AccessToken),
                        string.IsNullOrWhiteSpace(tokens.RefreshToken)
                            ? null
                            : encryption.Encrypt(tokens.RefreshToken),
                        tokens.ExpiresAt)),
                cancellationToken)
            .ConfigureAwait(false);

        if (linked.IsFailure)
        {
            return Result<Guid>.Failure(linked.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<Guid>.Success(linked.Value.Id);
    }
}
