using Lagedra.Modules.ActivationAndBilling.Domain.ValueObjects;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Policies;

public static class BillingPolicy
{
    public static ProrationWindow ComputeProration(DateTime startDate, DateTime endDate, long monthlyFeeCents)
    {
        return new ProrationWindow(startDate, endDate, monthlyFeeCents);
    }
}
