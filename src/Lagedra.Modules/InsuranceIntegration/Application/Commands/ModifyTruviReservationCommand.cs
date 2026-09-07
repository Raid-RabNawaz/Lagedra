using Lagedra.Modules.InsuranceIntegration.Application.Services;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.InsuranceIntegration.Application.Commands;

public sealed record ModifyTruviReservationCommand(
    Guid DealId,
    Guid CallerUserId,
    bool CallerIsAdmin) : IRequest<Result>;

public sealed class ModifyTruviReservationCommandHandler(
    IDealApplicationStatusProvider deals,
    TruviScreeningService screening)
    : IRequestHandler<ModifyTruviReservationCommand, Result>
{
    public async Task<Result> Handle(
        ModifyTruviReservationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var access = await TruviDealAccess.AuthorizeAsync(
            deals, request.DealId, request.CallerUserId, request.CallerIsAdmin, cancellationToken)
            .ConfigureAwait(false);
        if (access.IsFailure)
        {
            return access;
        }

        return await screening.ModifyForDealAsync(request.DealId, cancellationToken)
            .ConfigureAwait(false);
    }
}
