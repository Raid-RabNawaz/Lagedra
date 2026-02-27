using Lagedra.Infrastructure.Eventing;
using Lagedra.Modules.IdentityAndVerification.Domain.Events;
using Lagedra.Modules.InsuranceIntegration.Domain.Events;
using Lagedra.Modules.PartnerNetwork.Domain.Events;
using Lagedra.Modules.VerificationAndRisk.Application.EventHandlers;
using Lagedra.Modules.VerificationAndRisk.Infrastructure.Persistence;
using Lagedra.Modules.VerificationAndRisk.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lagedra.Modules.VerificationAndRisk;

public static class VerificationAndRiskModuleRegistration
{
    public static IServiceCollection AddVerificationAndRisk(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<RiskDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<RiskDbContext>();

        services.AddScoped<RiskProfileRepository>();

        services.AddDomainEventHandler<ReferralRedeemedEvent,
            OnReferralRedeemedRecalculateRiskHandler>();

        services.AddDomainEventHandler<IdentityVerifiedEvent,
            OnIdentityVerifiedRecalculateRiskHandler>();

        services.AddDomainEventHandler<BackgroundCheckReceivedEvent,
            OnBackgroundCheckReceivedRecalculateRiskHandler>();

        services.AddDomainEventHandler<InsuranceStatusChangedEvent,
            OnInsuranceStatusChangedRecalculateRiskHandler>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(VerificationAndRiskModuleRegistration).Assembly));

        return services;
    }
}
