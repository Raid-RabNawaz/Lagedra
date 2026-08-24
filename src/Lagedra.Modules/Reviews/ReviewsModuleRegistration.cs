using Lagedra.Infrastructure.Eventing;
using Lagedra.Modules.Reviews.Application.Commands;
using Lagedra.Modules.Reviews.Application.EventHandlers;
using Lagedra.Modules.Reviews.Domain.Events;
using Lagedra.Modules.Reviews.Infrastructure.Persistence;
using Lagedra.Modules.Reviews.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Integration.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        return services;
    }
}
