using System.Security.Claims;

namespace Lagedra.Modules.IdentityAndVerification.Application;

public static class HostStripeClaimReader
{
    public static string? Email(ClaimsPrincipal? user)
    {
        if (user is null)
        {
            return null;
        }

        foreach (var value in new[]
        {
            user.FindFirstValue(ClaimTypes.Email),
            user.FindFirstValue("email"),
        })
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
