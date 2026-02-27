using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.Modules.InsuranceIntegration.Application.EventHandlers;
using Lagedra.Modules.InsuranceIntegration.Application.Services;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Jobs;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Repositories;
using Lagedra.Infrastructure.Eventing;
using Lagedra.SharedKernel.Insurance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Lagedra.Modules.InsuranceIntegration;

public static class InsuranceIntegrationModuleRegistration
{
    public static IServiceCollection AddInsuranceIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<InsuranceDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<InsuranceDbContext>();

        services.AddScoped<InsurancePolicyRecordRepository>();

        var feeMode = configuration["Insurance:FeeCalculationMode"];
        if (string.Equals(feeMode, "Api", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IInsuranceFeeCalculator, ApiInsuranceFeeCalculator>();
        }
        else
        {
            services.AddScoped<IInsuranceFeeCalculator, ConfigurableInsuranceFeeCalculator>();
        }

        services.AddDomainEventHandler<DealActivatedEvent,
            OnDealActivatedActivateInsuranceHandler>();

        // Notification handlers
        services.AddDomainEventHandler<
            Lagedra.Modules.InsuranceIntegration.Domain.Events.InsuranceStatusChangedEvent,
            OnInsuranceStatusChangedNotify>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(InsuranceIntegrationModuleRegistration).Assembly));

        services.AddQuartz(q =>
        {
            var pollerKey = new JobKey("InsurancePoller");
            q.AddJob<InsurancePollerJob>(opts => opts.WithIdentity(pollerKey));
            q.AddTrigger(opts => opts
                .ForJob(pollerKey)
                .WithIdentity("InsurancePoller-trigger")
                .WithCronSchedule("0 0 * ? * *"));

            var slaKey = new JobKey("InsuranceUnknownSla");
            q.AddJob<InsuranceUnknownSlaJob>(opts => opts.WithIdentity(slaKey));
            q.AddTrigger(opts => opts
                .ForJob(slaKey)
                .WithIdentity("InsuranceUnknownSla-trigger")
                .WithCronSchedule("0 */30 * ? * *"));
        });

        return services;
    }
}
