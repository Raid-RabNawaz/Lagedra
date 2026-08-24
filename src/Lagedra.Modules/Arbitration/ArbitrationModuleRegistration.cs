using Lagedra.Infrastructure.Eventing;
using Lagedra.Modules.Arbitration.Application.Services;
using Lagedra.Modules.Arbitration.Application.EventHandlers;
using Lagedra.Modules.Arbitration.Domain.Events;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.Modules.Arbitration.Infrastructure.Repositories;
using Lagedra.Modules.Arbitration.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<ArbitratorAssignmentSelector>();
        services.AddScoped<ArbitrationCaseAccessEvaluator>();
        services.AddScoped<IArbitrationEvidenceManifestAccessProvider, ArbitrationEvidenceManifestAccessProvider>();

        // Notification handlers
        services.AddDomainEventHandler<CaseFiledEvent, OnCaseFiledNotify>();
        services.AddDomainEventHandler<DecisionIssuedEvent, OnDecisionIssuedNotify>();
        services.AddDomainEventHandler<EvidenceCompleteEvent, OnEvidenceCompleteNotify>();
        services.AddDomainEventHandler<CaseClosedEvent, OnCaseClosedNotify>();
        services.AddDomainEventHandler<CaseAppealedEvent, OnCaseAppealedNotify>();
        services.AddDomainEventHandler<ArbitrationBacklogEscalationEvent, OnBacklogEscalationHandler>();

        // Activates a case once its filing fee is paid (Stripe webhook → integration event).
        services.AddDomainEventHandler<ArbitrationFilingFeePaidEvent, OnArbitrationFilingFeePaidHandler>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ArbitrationModuleRegistration).Assembly));

        return services;
    }
}
