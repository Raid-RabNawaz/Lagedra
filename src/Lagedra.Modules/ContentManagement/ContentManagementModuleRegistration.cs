using Lagedra.Modules.ContentManagement.Infrastructure.Persistence;
using Lagedra.Modules.ContentManagement.Infrastructure.Repositories;
using Lagedra.Infrastructure.Eventing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lagedra.Modules.ContentManagement;

public static class ContentManagementModuleRegistration
{
    public static IServiceCollection AddContentManagement(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<ContentDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<ContentDbContext>();

        services.AddScoped<BlogPostRepository>();
        services.AddScoped<SeoPageRepository>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ContentManagementModuleRegistration).Assembly));

        return services;
    }
}
