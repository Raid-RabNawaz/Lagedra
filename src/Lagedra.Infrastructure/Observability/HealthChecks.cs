using Lagedra.Infrastructure.External.Antivirus;
using Lagedra.Infrastructure.External.Geocoding;
using Lagedra.Infrastructure.External.Payments;
using Lagedra.Infrastructure.External.Persona;
using Lagedra.Infrastructure.External.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lagedra.Infrastructure.Observability;

public static class InfrastructureHealthChecks
{
    public static IServiceCollection AddInfrastructureHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        var builder = services.AddHealthChecks();

        builder.AddNpgSql(
            connectionString,
            name: "postgres",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["db", "infrastructure"]);

        builder.AddCheck<MinioHealthCheck>(
            "minio",
            failureStatus: HealthStatus.Degraded,
            tags: ["storage", "infrastructure"]);

        builder.AddCheck<ClamAvHealthCheck>(
            "clamav",
            failureStatus: HealthStatus.Degraded,
            tags: ["antivirus", "infrastructure"]);

        builder.AddCheck<StripeHealthCheck>(
            "stripe",
            failureStatus: HealthStatus.Degraded,
            tags: ["payments", "external"]);

        builder.AddCheck<GoogleMapsHealthCheck>(
            "google-maps",
            failureStatus: HealthStatus.Degraded,
            tags: ["geocoding", "external"]);

        builder.AddCheck<PersonaHealthCheck>(
            "persona",
            failureStatus: HealthStatus.Degraded,
            tags: ["kyc", "external"]);

        return services;
    }
}

public sealed class MinioHealthCheck(IObjectStorageService storage) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await storage.EnsureBucketExistsAsync("lagedra-health-probe", cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy("MinIO is reachable.");
        }
#pragma warning disable CA1031 // intentional: health check must not throw
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return HealthCheckResult.Degraded("MinIO is unreachable.", ex);
        }
    }
}

public sealed class ClamAvHealthCheck(IAntivirusService antivirus) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var empty = new MemoryStream([]);
            var result = await antivirus.ScanAsync(empty, cancellationToken).ConfigureAwait(false);
            return result.Status == ScanStatus.Error
                ? HealthCheckResult.Degraded("ClamAV returned an error on probe scan.")
                : HealthCheckResult.Healthy("ClamAV is reachable.");
        }
#pragma warning disable CA1031 // intentional: health check must not throw
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return HealthCheckResult.Degraded("ClamAV is unreachable.", ex);
        }
    }
}

public sealed class StripeHealthCheck(IStripeService stripe) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await stripe.GetOrCreateCustomerAsync(Guid.Empty, "healthcheck@lagedra.internal", cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy("Stripe API is reachable.");
        }
#pragma warning disable CA1031 // intentional: health check must not throw
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return HealthCheckResult.Degraded("Stripe API is unreachable.", ex);
        }
    }
}

public sealed class GoogleMapsHealthCheck(IGeocodingService geocoding) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await geocoding.GeocodeAddressAsync("1600 Amphitheatre Pkwy, Mountain View, CA", cancellationToken).ConfigureAwait(false);
            return result is not null
                ? HealthCheckResult.Healthy("Google Maps API is reachable.")
                : HealthCheckResult.Degraded("Google Maps API returned no results.");
        }
#pragma warning disable CA1031 // intentional: health check must not throw
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return HealthCheckResult.Degraded("Google Maps API is unreachable.", ex);
        }
    }
}

public sealed class PersonaHealthCheck(IPersonaClient persona) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await persona.GetInquiryAsync("health-check-probe", cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy("Persona API is reachable.");
        }
#pragma warning disable CA1031 // intentional: health check must not throw
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return HealthCheckResult.Degraded("Persona API is unreachable.", ex);
        }
    }
}
