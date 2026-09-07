using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;

namespace Lagedra.Modules.InsuranceIntegration.Application.Commands;

internal static class TruviDealAccess
{
    private static readonly Error NotFound = new(
        "Insurance.DealNotFound",
        "Deal application was not found.");
    private static readonly Error Forbidden = new(
        "Insurance.Forbidden",
        "You are not allowed to change stay protection for this deal.");

    public static Task<Result> AuthorizeAsync(
        IDealApplicationStatusProvider deals,
        Guid dealId,
        Guid callerUserId,
        bool callerIsAdmin,
        CancellationToken cancellationToken)
        => AuthorizeAsync(deals, dealId, callerUserId, callerIsAdmin, hostOnly: false, cancellationToken);

    public static Task<Result> AuthorizeHostAsync(
        IDealApplicationStatusProvider deals,
        Guid dealId,
        Guid callerUserId,
        bool callerIsAdmin,
        CancellationToken cancellationToken)
        => AuthorizeAsync(deals, dealId, callerUserId, callerIsAdmin, hostOnly: true, cancellationToken);

    private static async Task<Result> AuthorizeAsync(
        IDealApplicationStatusProvider deals,
        Guid dealId,
        Guid callerUserId,
        bool callerIsAdmin,
        bool hostOnly,
        CancellationToken cancellationToken)
    {
        var deal = await deals.GetDealDetailsAsync(dealId, cancellationToken).ConfigureAwait(false);
        if (deal is null)
        {
            return Result.Failure(NotFound);
        }

        if (callerIsAdmin
            || callerUserId == deal.LandlordUserId
            || callerUserId == deal.HomeOwnerUserId
            || (!hostOnly && callerUserId == deal.TenantUserId))
        {
            return Result.Success();
        }

        return Result.Failure(Forbidden);
    }
}
