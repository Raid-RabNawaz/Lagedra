using Lagedra.Infrastructure.Eventing;
using Lagedra.Modules.Reviews.Application.Commands;
using Lagedra.Modules.Reviews.Application.EventHandlers;
using Lagedra.Modules.Reviews.Domain.Events;
using Lagedra.Modules.Reviews.Infrastructure.Jobs;
using Lagedra.Modules.Reviews.Infrastructure.Persistence;
using Lagedra.Modules.Reviews.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Integration.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Lagedra.Modules.Reviews;

public static class ReviewsModuleRegistration
{
    public static IServiceCollection AddReviews(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<ReviewsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<ReviewsDbContext>();

        services.AddScoped<IReviewReputationProvider, ReviewReputationProvider>();

        services.AddDomainEventHandler<StayCompletedEvent, OnStayCompletedOpenReviewWindowHandler>();
        services.AddDomainEventHandler<StayReviewWindowOpenedEvent, OnStayReviewWindowOpenedNotify>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ReviewsModuleRegistration).Assembly));

        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("PublishExpiredStayReviews");
            q.AddJob<PublishExpiredStayReviewsJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("PublishExpiredStayReviews-trigger")
                .WithCronSchedule("0 0 8,20 ? * *")); // twice daily 08:00 and 20:00 UTC
        });

        return services;
    }
}
