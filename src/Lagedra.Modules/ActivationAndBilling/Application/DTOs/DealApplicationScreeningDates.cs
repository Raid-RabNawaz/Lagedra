using Lagedra.SharedKernel.Integration;

namespace Lagedra.Modules.ActivationAndBilling.Application.DTOs;

internal static class DealApplicationScreeningDates
{
    public static async Task<IReadOnlyList<DealApplicationDto>> AttachAsync(
        IReadOnlyList<DealApplicationDto> dtos,
        IVerificationSignalProvider verificationSignals,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dtos);
        ArgumentNullException.ThrowIfNull(verificationSignals);

        if (dtos.Count == 0)
        {
            return dtos;
        }

        var tenantIds = dtos.Select(d => d.TenantUserId).Distinct().ToList();
        var times = await verificationSignals
            .GetLatestScreeningTimesAsync(tenantIds, cancellationToken)
            .ConfigureAwait(false);

        var applicationIds = dtos.Select(d => d.ApplicationId).Distinct().ToList();
        var orders = await verificationSignals
            .GetLatestScreeningOrdersAsync(applicationIds, cancellationToken)
            .ConfigureAwait(false);

        return dtos
            .Select(d =>
            {
                var next = times.TryGetValue(d.TenantUserId, out var at)
                    ? d with { LastScreenedAt = at }
                    : d;
                if (orders.TryGetValue(d.ApplicationId, out var order))
                {
                    next = next with
                    {
                        ScreeningOrderStatus = order.Status,
                        ScreeningResult = order.Result,
                        ScreeningRequestedByHostUserId = order.HostUserId
                    };
                }

                return next;
            })
            .ToList();
    }
}
