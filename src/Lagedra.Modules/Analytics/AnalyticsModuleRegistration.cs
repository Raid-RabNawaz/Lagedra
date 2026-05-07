using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Lagedra.Modules.Analytics;

public static class AnalyticsModuleRegistration
{
    public static IServiceCollection AddAnalytics(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Analytics queries use raw ADO.NET via NpgsqlDataSource for cross-module
        // aggregations (each domain module owns its own DbContext, so we can't
        // share an EF context here). Register the data source as a singleton if
        // no other module has already done so.
        services.TryAddSingleton<NpgsqlDataSource>(_ =>
        {
            var connectionString = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException(
                    "Connection string 'Default' is required for the Analytics module.");
            var builder = new NpgsqlDataSourceBuilder(connectionString);
            builder.EnableDynamicJson();
            return builder.Build();
        });

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AnalyticsModuleRegistration).Assembly));

        return services;
    }
}
