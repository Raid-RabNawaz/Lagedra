using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Lagedra.Infrastructure.Security;

public static class DataProtectionSetup
{
    /// <summary>
    /// Persists Data Protection keys to the local filesystem volume.
    /// In production, mount a Docker volume at the configured path so keys
    /// survive container restarts. For multi-instance deployments, switch to
    /// a shared store (Redis or PostgreSQL) before scaling beyond one replica.
    /// </summary>
    public static IServiceCollection AddLagedraDataProtection(
        this IServiceCollection services,
        string keysPath = "/app/data-protection-keys")
    {
        ArgumentNullException.ThrowIfNull(services);

        DataProtectionServiceCollectionExtensions
            .AddDataProtection(services)
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
            .SetApplicationName("Lagedra");

        return services;
    }
}
