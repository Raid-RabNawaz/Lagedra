using Lagedra.Infrastructure.Eventing;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Jobs;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Lagedra.Modules.StructuredInquiry;

public static class StructuredInquiryModuleRegistration
{
    public static IServiceCollection AddStructuredInquiry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<InquiryDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<InquiryDbContext>();

        services.AddScoped<InquirySessionRepository>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(StructuredInquiryModuleRegistration).Assembly));

        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("InquiryIntegrityScan");
            q.AddJob<InquiryIntegrityScanJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("InquiryIntegrityScan-trigger")
                .WithCronSchedule("0 0 2 ? * *"));
        });

        return services;
    }
}
