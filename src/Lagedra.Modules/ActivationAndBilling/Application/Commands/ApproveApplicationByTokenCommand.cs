using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Security;
using MediatR;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

/// <summary>
/// Phase 16.10 — one-tap approval triggered by the host clicking the
/// deep link in their booking notification email. The token encodes
/// the (action, applicationId, hostUserId, expiry) tuple; the handler
/// validates, then forwards to the existing
/// <see cref="ApproveDealApplicationCommand"/> so all downstream
/// behaviours (Truth Surface auto-confirm, card-on-file off-session
/// charge, notifications) run identically to the in-app path.
/// </summary>
public sealed record ApproveApplicationByTokenCommand(
    string Token,
    long? DepositAmountCentsOverride = null) : IRequest<Result<DealApplicationDto>>;

public sealed class ApproveApplicationByTokenCommandHandler(
    IActionTokenService actionTokens,
    IMediator mediator)
    : IRequestHandler<ApproveApplicationByTokenCommand, Result<DealApplicationDto>>
{
    public const string ActionLabel = "approve_application";

    public async Task<Result<DealApplicationDto>> Handle(
        ApproveApplicationByTokenCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate the deposit *before* burning the token's single-use
        // budget — otherwise a host who hits "Approve" with the deposit
        // field empty would invalidate their email link and have to ask
        // support to reissue.
        var deposit = request.DepositAmountCentsOverride ?? 0;
        if (deposit <= 0)
        {
            return Result<DealApplicationDto>.Failure(new Error(
                "OneTap.MissingDeposit",
                "A deposit amount is required to one-tap approve."));
        }

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
                deposit),
            cancellationToken).ConfigureAwait(false);
    }
}
