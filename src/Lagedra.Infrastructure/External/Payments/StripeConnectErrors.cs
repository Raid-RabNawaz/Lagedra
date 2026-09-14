using Stripe;

namespace Lagedra.Infrastructure.External.Payments;

public static class StripeConnectErrors
{
    public static bool IsMissingAccount(StripeException ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        var message = ex.StripeError?.Message ?? ex.Message ?? string.Empty;
        if (message.Contains("No such account", StringComparison.OrdinalIgnoreCase)
            || message.Contains("does not have access to account", StringComparison.OrdinalIgnoreCase)
            || message.Contains("that account does not exist", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(ex.StripeError?.Code, "resource_missing", StringComparison.Ordinal)
               && message.Contains("account", StringComparison.OrdinalIgnoreCase);
    }
}
