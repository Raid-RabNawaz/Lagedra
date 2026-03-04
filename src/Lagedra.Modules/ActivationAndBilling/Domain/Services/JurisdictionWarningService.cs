namespace Lagedra.Modules.ActivationAndBilling.Domain.Services;

public static class JurisdictionWarningService
{
    public static string? CheckForWarnings(string? jurisdictionCode, int stayDurationDays)
    {
        if (string.Equals(jurisdictionCode, "US-CA-LA", StringComparison.OrdinalIgnoreCase)
            && stayDurationDays > 175)
        {
            return "AB 1482 'Just Cause': Stays exceeding 175 days in Los Angeles " +
                   "may trigger tenant protections under the Tenant Protection Act. " +
                   "Review legal obligations before proceeding.";
        }

        return null;
    }
}
