using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Application.Services;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Security;
using MediatR;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record ConsentOwnerTenancyByTokenCommand(
    string Token,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<DealApplicationDto>>;

public sealed class ConsentOwnerTenancyByTokenCommandHandler(
    IActionTokenService actionTokens,
    IMediator mediator)
    : IRequestHandler<ConsentOwnerTenancyByTokenCommand, Result<DealApplicationDto>>
{
    public const string ActionLabel = "consent_owner_tenancy";

    public async Task<Result<DealApplicationDto>> Handle(
        ConsentOwnerTenancyByTokenCommand request,
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
            new ConsentOwnerTenancyCommand(
                validation.Payload.SubjectId,
                validation.Payload.PrincipalUserId,
                ConsentGiven: true,
                ConsentVersion: OwnerTenancyConsent.EmailOneTapVersion,
                IpAddress: request.IpAddress,
                UserAgent: request.UserAgent),
            cancellationToken).ConfigureAwait(false);
    }
}
