using Lagedra.SharedKernel.Results;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Infrastructure.External.Payments;

/// <summary>
/// Validates Stripe Connect onboarding return/refresh URLs so callers cannot
/// redirect users to arbitrary third-party sites.
/// </summary>
public static class StripeConnectUrlValidator
{
    private const string PayoutSetupPath = "/app/payout-setup";

    public static Result<Uri> ValidateOrDefault(
        Uri? url,
        Uri fallback,
        IConfiguration configuration)
    {
        if (url is null)
        {
            return Result<Uri>.Success(fallback);
        }

        if (!IsAllowed(url, configuration))
        {
            return Result<Uri>.Failure(
                new Error(
                    "StripeConnect.ForbiddenUrl",
                    "Return URL must point to this app's payout setup page."));
        }

        return Result<Uri>.Success(url);
    }

    private static bool IsAllowed(Uri uri, IConfiguration configuration)
    {
        if (!uri.AbsolutePath.StartsWith(PayoutSetupPath, StringComparison.Ordinal))
        {
            return false;
        }

        var configuredFrontend = configuration["App:FrontendUrl"];
        if (Uri.TryCreate(configuredFrontend, UriKind.Absolute, out var frontend)
            && string.Equals(uri.Host, frontend.Host, StringComparison.OrdinalIgnoreCase)
            && uri.Scheme.Equals(frontend.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Local dev: allow any localhost / loopback port (Vite, Docker, etc.).
        return uri.Scheme is "http" or "https"
            && uri.Host is "localhost" or "127.0.0.1";
    }
}
