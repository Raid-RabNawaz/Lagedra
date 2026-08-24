using Quartz;

namespace Lagedra.Worker.Scheduling;

/// <summary>
/// Runs several module job "tasks" sequentially under a single Quartz
/// trigger, so one scheduled job can do the work of many. Children are the
/// unchanged module job classes — they are resolved from DI as plain scoped
/// services rather than being scheduled individually.
///
/// Failure semantics: each child is isolated (a failure is logged and the
/// remaining children still run), then the composite rethrows an aggregate
/// so Quartz records the execution as failed and the HealthOrchestrator
/// failure counters still fire.
/// </summary>
internal abstract class SequentialCompositeJob(ILogger logger) : IJob
{
    protected abstract IReadOnlyList<IJob> Children { get; }

    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        List<Exception>? failures = null;

        foreach (var child in Children)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            try
            {
                await child.Execute(context).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
#pragma warning disable CA1031 // one failed task must not stop the remaining tasks
            catch (Exception ex)
#pragma warning restore CA1031
            {
                (failures ??= []).Add(ex);
                logger.LogError(
                    ex,
                    "Composite task {ChildTask} failed in {CompositeJob} — continuing with remaining tasks",
                    child.GetType().Name,
                    GetType().Name);
            }
        }

        if (failures is { Count: > 0 })
        {
            throw new AggregateException(
                $"{failures.Count} of {Children.Count} task(s) failed in {GetType().Name}.",
                failures);
        }
    }
}
