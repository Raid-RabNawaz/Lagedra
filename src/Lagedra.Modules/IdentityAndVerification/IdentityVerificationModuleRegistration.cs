using Lagedra.Modules.IdentityAndVerification.Application.EventHandlers;
using Lagedra.Modules.IdentityAndVerification.Domain.Events;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Jobs;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Repositories;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Services;
using Lagedra.Infrastructure.Eventing;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Lagedra.Modules.IdentityAndVerification;

public static class IdentityVerificationModuleRegistration
{
    public static IServiceCollection AddIdentityVerification(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<IdentityDbContext>();

        services.AddScoped<IdentityProfileRepository>();
        services.AddScoped<VerificationCaseRepository>();
        services.AddScoped<HostPaymentDetailsRepository>();
        services.AddScoped<IHostPaymentDetailsProvider, HostPaymentDetailsProvider>();

        // Notification handlers
        services.AddDomainEventHandler<IdentityVerifiedEvent, OnIdentityVerifiedNotify>();
        services.AddDomainEventHandler<IdentityVerificationFailedEvent, OnIdentityVerificationFailedNotify>();
        services.AddDomainEventHandler<VerificationClassChangedEvent, OnVerificationClassChangedNotify>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(IdentityVerificationModuleRegistration).Assembly));

        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("FraudFlagSlaMonitor");
            q.AddJob<FraudFlagSlaMonitorJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("FraudFlagSlaMonitor-trigger")
                .WithSimpleSchedule(s => s
                    .WithIntervalInMinutes(15)
                    .RepeatForever()));
        });

        return services;
    }
}
