using Lagedra.Infrastructure.Eventing;
using Lagedra.Modules.ComplianceMonitoring.Application.EventHandlers;
using Lagedra.Modules.ComplianceMonitoring.Infrastructure.Persistence;
using Lagedra.Modules.ComplianceMonitoring.Infrastructure.Repositories;
using Lagedra.SharedKernel.Integration.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lagedra.Modules.ComplianceMonitoring;

public static class ComplianceMonitoringModuleRegistration
{
    public static IServiceCollection AddComplianceMonitoring(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<ComplianceMonitoringDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<ComplianceMonitoringDbContext>();

        services.AddScoped<MonitoredViolationRepository>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ComplianceMonitoringModuleRegistration).Assembly));

        services.AddDomainEventHandler<InsuranceStatusChangedEvent, OnInsuranceStatusChangedRecordSignalHandler>();
        services.AddDomainEventHandler<BillingStoppedEvent, OnBillingStoppedRecordSignalHandler>();
        services.AddDomainEventHandler<RentMissedEvent, OnRentMissedRecordSignalHandler>();

        return services;
    }
}
