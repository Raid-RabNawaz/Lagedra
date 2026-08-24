using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Security;
using MediatR;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record DeclineOwnerTenancyByTokenCommand(string Token)
    : IRequest<Result<DealApplicationDto>>;

public sealed class DeclineOwnerTenancyByTokenCommandHandler(
    IActionTokenService actionTokens,
    IMediator mediator)
    : IRequestHandler<DeclineOwnerTenancyByTokenCommand, Result<DealApplicationDto>>
{
    public const string ActionLabel = "decline_owner_tenancy";

    public async Task<Result<DealApplicationDto>> Handle(
        DeclineOwnerTenancyByTokenCommand request,
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
            new DeclineOwnerTenancyCommand(
                validation.Payload.SubjectId,
                validation.Payload.PrincipalUserId),
            cancellationToken).ConfigureAwait(false);
    }
}
