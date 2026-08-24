using Lagedra.Infrastructure.Eventing;
using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Lagedra.Modules.Notifications.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Lagedra.Modules.Notifications;

public static class NotificationsModuleRegistration
{
    public static IServiceCollection AddNotifications(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(
            configuration.GetConnectionString("Default"));
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(dataSource));

        services.AddOutboxContext<NotificationDbContext>();

        services.AddScoped<NotificationRepository>();
        services.AddScoped<TemplateRepository>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(NotificationsModuleRegistration).Assembly));

        return services;
    }
}
