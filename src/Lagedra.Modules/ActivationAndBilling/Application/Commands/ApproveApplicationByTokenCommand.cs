using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Application.Services;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Security;
using MediatR;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

/// <summary>
/// One-tap approval triggered by the host clicking the deep link in their
/// booking notification email. The token encodes the (action, applicationId,
/// hostUserId, expiry) tuple; the handler validates, then forwards to
/// <see cref="ApproveDealApplicationCommand"/> so all downstream behaviours
/// (atomic Truth Surface seal, off-session charge, notifications) run
/// identically to the in-app path. No deposit is required — it was
/// predetermined at request time. Clicking the signed link constitutes the
/// host's Truth Surface consent.
/// </summary>
public sealed record ApproveApplicationByTokenCommand(
    string Token,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<DealApplicationDto>>;

public sealed class ApproveApplicationByTokenCommandHandler(
    IActionTokenService actionTokens,
    IMediator mediator)
    : IRequestHandler<ApproveApplicationByTokenCommand, Result<DealApplicationDto>>
{
    public const string ActionLabel = "approve_application";
    public const string ConsentVersion = "ts-consent-email-one-tap-v1";

    public async Task<Result<DealApplicationDto>> Handle(
        ApproveApplicationByTokenCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = await actionTokens
            .ValidateAndConsumeAsync(request.Token, ActionLabel, cancellationToken)
            .ConfigureAwait(false);
        if (!validation.IsValid || validation.Payload is null)
        {
            return Result<DealApplicationDto>.Failure(new Error(
                $"OneTap.{validation.ErrorCode}",
                validation.ErrorMessage ?? "Action token is invalid."));
        }

        return await mediator.Send(
            new ApproveDealApplicationCommand(
                validation.Payload.SubjectId,
                validation.Payload.PrincipalUserId,
                TruthSurfaceConsentGiven: true,
                ConsentVersion: ConsentVersion,
                IpAddress: request.IpAddress,
                UserAgent: request.UserAgent),
            cancellationToken).ConfigureAwait(false);
    }
}
