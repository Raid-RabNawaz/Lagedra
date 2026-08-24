using System.Reflection;
using Npgsql;
using Quartz;
using Quartz.Impl.Matchers;

namespace Lagedra.Worker.Scheduling;

/// <summary>
/// Runs before QuartzHostedService (hosted services start in registration
/// order) and makes scheduler startup self-sufficient:
///
/// 1. Creates the qrtz_* tables when they are missing. The worker previously
///    crash-looped for 15 days (Jul 8-23) because the persistent store was
///    enabled while the schema had never been applied to the production
///    database — schema validation threw in StartAsync, the host died, and
///    ECS restarted it every ~50 seconds. The schema was eventually applied
///    by hand; this service removes that manual step for every future
///    environment.
///
/// 2. Deletes jobs from the persistent store whose keys are no longer in
///    JobRegistry. AddJob/AddTrigger only upsert — they never remove — so
///    without this, keys from older deployments (e.g. the retired per-module
///    self-registrations like "InsurancePoller") would keep firing forever
///    alongside the current "InsurancePollerJob" keys, which is exactly what
///    made every job execute twice.
/// </summary>
internal sealed partial class QuartzBootstrapService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<QuartzBootstrapService> logger) : IHostedService
{
    private const string SchemaResourceName = "Lagedra.Worker.Scheduling.quartz_tables_postgres.sql";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is required.");

        await EnsureSchemaAsync(connectionString, cancellationToken).ConfigureAwait(false);
        await PurgeStaleJobsAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureSchemaAsync(string connectionString, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);

        // Cast to text: Npgsql 9+ cannot read PostgreSQL regclass via ExecuteScalar as System.Object.
        await using (var check = new NpgsqlCommand(
            "SELECT to_regclass('public.qrtz_job_details')::text", connection))
        {
            var existing = await check.ExecuteScalarAsync(ct).ConfigureAwait(false);
            if (existing is string { Length: > 0 })
            {
                LogSchemaPresent(logger);
                return;
            }
        }

        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream(SchemaResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{SchemaResourceName}' not found.");
        using var reader = new StreamReader(stream);
        var sql = await reader.ReadToEndAsync(ct).ConfigureAwait(false);

#pragma warning disable CA2100 // static DDL from an embedded resource, no user input
        await using var create = new NpgsqlCommand(sql, connection);
#pragma warning restore CA2100
        await create.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        LogSchemaCreated(logger);
    }

    private async Task PurgeStaleJobsAsync(CancellationToken ct)
    {
        var schedulerFactory = serviceProvider.GetRequiredService<ISchedulerFactory>();
        // Instantiates the scheduler (config is applied, it is not started yet;
        // QuartzHostedService starts this same instance afterwards).
        var scheduler = await schedulerFactory.GetScheduler(ct).ConfigureAwait(false);

        var keys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup(), ct).ConfigureAwait(false);
        foreach (var key in keys)
        {
            if (JobRegistry.ExpectedJobNames.Contains(key.Name))
            {
                continue;
            }

            await scheduler.DeleteJob(key, ct).ConfigureAwait(false);
            LogStaleJobRemoved(logger, key.Name, key.Group);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Quartz schema already present")]
    private static partial void LogSchemaPresent(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Quartz schema was missing and has been created (qrtz_* tables)")]
    private static partial void LogSchemaCreated(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Removed stale Quartz job {JobName} (group {JobGroup}) from the persistent store")]
    private static partial void LogStaleJobRemoved(ILogger logger, string jobName, string jobGroup);
}
