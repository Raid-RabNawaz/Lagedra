using System.Diagnostics.CodeAnalysis;
using Lagedra.Infrastructure.External.Channels.OwnerRez;
using Lagedra.Modules.ChannelIntegration.Domain.Enums;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Services;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ChannelIntegration.Application.Commands;

/// <summary>
/// Begins the OwnerRez OAuth flow by handing the host the URL of OwnerRez's
/// consent screen. Nothing is stored yet — the connection is created when
/// OwnerRez calls back with an authorization code.
/// </summary>
public sealed record StartOwnerRezOAuthCommand(Guid HostUserId)
    : IRequest<Result<OwnerRezAuthorizationDto>>;

/// <param name="AuthorizationUrl">Send the host here to approve access.</param>
[SuppressMessage("Design", "CA1054:URI-like parameters should not be strings",
    Justification = "Response DTO consumed by the frontend; serialized to JSON as a string.")]
[SuppressMessage("Design", "CA1056:URI-like properties should not be strings",
    Justification = "Response DTO consumed by the frontend; serialized to JSON as a string.")]
public sealed record OwnerRezAuthorizationDto(string AuthorizationUrl);

public sealed class StartOwnerRezOAuthCommandHandler(
    ChannelDbContext dbContext,
    OwnerRezOAuthFlow flow,
    OwnerRezOAuthClient oauthClient)
    : IRequestHandler<StartOwnerRezOAuthCommand, Result<OwnerRezAuthorizationDto>>
{
    private static readonly Error NotConfigured = new(
        "Channel.OwnerRezNotConfigured",
        "OwnerRez connections are not available right now. Please try again later.");

    public async Task<Result<OwnerRezAuthorizationDto>> Handle(
        StartOwnerRezOAuthCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!flow.IsConfigured)
        {
            return Result<OwnerRezAuthorizationDto>.Failure(NotConfigured);
        }

        // Checked here as well as on the callback so a host who already has
        // OwnerRez linked is told so up front, instead of authorizing at OwnerRez
        // and only then being refused.
        var existing = await dbContext.Connections
            .Where(c => c.HostUserId == request.HostUserId
                     && c.ProviderKey == OwnerRezOAuthFlow.ProviderKey
                     && c.Status != ChannelConnectionStatus.Revoked)
            .Select(c => c.DisplayName)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return Result<OwnerRezAuthorizationDto>.Failure(
                ChannelConnectionLinker.AlreadyConnected(existing));
        }

        var url = oauthClient.BuildAuthorizationUrl(
            flow.RedirectUri, flow.CreateState(request.HostUserId));

        return Result<OwnerRezAuthorizationDto>.Success(
            new OwnerRezAuthorizationDto(url.ToString()));
    }
}
