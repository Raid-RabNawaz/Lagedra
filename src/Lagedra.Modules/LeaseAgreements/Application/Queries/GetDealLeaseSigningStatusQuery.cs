using Lagedra.Modules.LeaseAgreements.Application.DTOs;
using Lagedra.Modules.LeaseAgreements.Application.Services;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.LeaseAgreements.Application.Queries;

public sealed record GetDealLeaseSigningStatusQuery(Guid DealId, Guid CallerUserId)
    : IRequest<Result<DealLeaseSigningStatusDto>>;

public sealed class GetDealLeaseSigningStatusQueryHandler(
    IDealApplicationStatusProvider deals,
    IListingProvider listings,
    IDealLeaseSignatureStore signatures)
    : IRequestHandler<GetDealLeaseSigningStatusQuery, Result<DealLeaseSigningStatusDto>>
{
    private static readonly Error NotFound = new("LeaseAgreement.DealNotFound", "Deal not found.");
    private static readonly Error Forbidden = new(
        "LeaseAgreement.Forbidden",
        "Only the host or tenant on this deal can view lease signatures.");

    public async Task<Result<DealLeaseSigningStatusDto>> Handle(
        GetDealLeaseSigningStatusQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var participants = await deals.GetParticipantsAsync(request.DealId, cancellationToken)
            .ConfigureAwait(false);
        if (participants is null)
        {
            return Result<DealLeaseSigningStatusDto>.Failure(NotFound);
        }

        var callerRole = DealLeaseSigning.RoleFor(request.CallerUserId, participants);
        if (callerRole is null)
        {
            return Result<DealLeaseSigningStatusDto>.Failure(Forbidden);
        }

        var deal = await deals.GetDealDetailsAsync(request.DealId, cancellationToken)
            .ConfigureAwait(false);
        var listing = deal is null
            ? null
            : await listings.GetListingDetailsAsync(deal.ListingId, cancellationToken)
                .ConfigureAwait(false);

        var rows = await signatures.ListByDealAsync(request.DealId, cancellationToken)
            .ConfigureAwait(false);

        return Result<DealLeaseSigningStatusDto>.Success(
            DealLeaseSigning.Status(DealLeaseSigning.IsRequired(listing), callerRole, rows));
    }
}
