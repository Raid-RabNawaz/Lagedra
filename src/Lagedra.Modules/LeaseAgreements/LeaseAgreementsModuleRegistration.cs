using Lagedra.Infrastructure.Eventing;
using Lagedra.Modules.LeaseAgreements.Application.EventHandlers;
using Lagedra.Modules.LeaseAgreements.Domain.Events;
using Lagedra.Modules.LeaseAgreements.Infrastructure.Persistence;
using Lagedra.Modules.LeaseAgreements.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Integration.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lagedra.Modules.LeaseAgreements;

public static class LeaseAgreementsModuleRegistration
{
    public static IServiceCollection AddLeaseAgreements(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<LeaseAgreementDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<LeaseAgreementDbContext>();

        services.AddScoped<ILeaseAgreementTemplateProvider, LeaseAgreementTemplateProvider>();
        services.AddScoped<ILeaseAgreementFiller, LeaseAgreementFiller>();
        services.AddScoped<IDealLeaseDocumentStore, DealLeaseDocumentStore>();
        services.AddSingleton<ILeasePdfGenerator, LeasePdfGenerator>();

        services.AddDomainEventHandler<LeaseAgreementTemplatePublishedEvent, OnTemplatePublishedInvalidateCacheHandler>();
        services.AddDomainEventHandler<TruthSurfaceConfirmedEvent, OnTruthSurfaceConfirmedGenerateLeasePdfHandler>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(LeaseAgreementsModuleRegistration).Assembly));

        return services;
    }
}
