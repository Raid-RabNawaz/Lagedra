using Lagedra.Modules.InsuranceIntegration.Application.Services;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.InsuranceIntegration.Application.Commands;

public sealed record RescreenTruviVerificationCommand(
    Guid DealId,
    Guid CallerUserId,
    bool CallerIsAdmin) : IRequest<Result>;

public sealed class RescreenTruviVerificationCommandHandler(
    IDealApplicationStatusProvider deals,
    TruviScreeningService screening)
    : IRequestHandler<RescreenTruviVerificationCommand, Result>
{
    public async Task<Result> Handle(
        RescreenTruviVerificationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var access = await TruviDealAccess.AuthorizeHostAsync(
            deals, request.DealId, request.CallerUserId, request.CallerIsAdmin, cancellationToken)
            .ConfigureAwait(false);
        if (access.IsFailure)
        {
            return access;
        }

        return await screening.RescreenForDealAsync(request.DealId, cancellationToken)
            .ConfigureAwait(false);
    }
}
