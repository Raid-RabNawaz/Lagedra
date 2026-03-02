using Lagedra.Infrastructure.Eventing;
using Lagedra.Modules.Arbitration.Application.EventHandlers;
using Lagedra.Modules.Arbitration.Domain.Events;
using Lagedra.Modules.Arbitration.Infrastructure.Jobs;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.Modules.Arbitration.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Lagedra.Modules.Arbitration;

public static class ArbitrationModuleRegistration
{
    public static IServiceCollection AddArbitration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<ArbitrationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<ArbitrationDbContext>();

        services.AddScoped<ArbitrationCaseRepository>();

        // Notification handlers
        services.AddDomainEventHandler<CaseFiledEvent, OnCaseFiledNotify>();
        services.AddDomainEventHandler<DecisionIssuedEvent, OnDecisionIssuedNotify>();
        services.AddDomainEventHandler<EvidenceCompleteEvent, OnEvidenceCompleteNotify>();
        services.AddDomainEventHandler<CaseClosedEvent, OnCaseClosedNotify>();
        services.AddDomainEventHandler<CaseAppealedEvent, OnCaseAppealedNotify>();
        services.AddDomainEventHandler<ArbitrationBacklogEscalationEvent, OnBacklogEscalationHandler>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ArbitrationModuleRegistration).Assembly));

        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("ArbitrationBacklogSla");
            q.AddJob<ArbitrationBacklogSlaJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("ArbitrationBacklogSla-trigger")
                .WithCronSchedule("0 0 * ? * *"));
        });

        return services;
    }
}
