using Lagedra.Infrastructure.Eventing;
using Lagedra.Modules.Notifications.Infrastructure.Jobs;
using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Lagedra.Modules.Notifications.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Lagedra.Modules.Notifications;

public static class NotificationsModuleRegistration
{
    public static IServiceCollection AddNotifications(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<NotificationDbContext>();

        services.AddScoped<NotificationRepository>();
        services.AddScoped<TemplateRepository>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(NotificationsModuleRegistration).Assembly));

        services.AddQuartz(q =>
        {
            var retryKey = new JobKey("NotificationRetry");
            q.AddJob<NotificationRetryJob>(opts => opts.WithIdentity(retryKey));
            q.AddTrigger(opts => opts
                .ForJob(retryKey)
                .WithIdentity("NotificationRetry-trigger")
                .WithSimpleSchedule(s => s
                    .WithIntervalInMinutes(10)
                    .RepeatForever()));

            var processingKey = new JobKey("NotificationProcessing");
            q.AddJob<NotificationProcessingJob>(opts => opts.WithIdentity(processingKey));
            q.AddTrigger(opts => opts
                .ForJob(processingKey)
                .WithIdentity("NotificationProcessing-trigger")
                .WithSimpleSchedule(s => s
                    .WithIntervalInSeconds(30)
                    .RepeatForever()));
        });

        return services;
    }
}
