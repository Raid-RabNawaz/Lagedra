using Lagedra.Infrastructure.Eventing;
using Lagedra.Modules.StructuredInquiry.Application.EventHandlers;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Jobs;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Repositories;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Integration.Events;
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
        services.AddScoped<IInquiryDealLinker, InquiryDealLinker>();
        services.AddScoped<IAcceptedInquiryOfferProvider, AcceptedInquiryOfferProvider>();

        services.AddDomainEventHandler<TruthSurfaceConfirmedEvent,
            OnTruthSurfaceConfirmedCloseInquiryHandler>();

        services.AddDomainEventHandler<Domain.Events.ListingInquiryStartedEvent,
            OnListingInquiryStartedNotifyHandler>();

        services.AddDomainEventHandler<Domain.Events.InquiryOfferProposedEvent,
            OnInquiryOfferProposedNotifyHandler>();

        services.AddDomainEventHandler<Domain.Events.InquiryOfferAcceptedEvent,
            OnInquiryOfferAcceptedNotifyHandler>();

        services.AddDomainEventHandler<Domain.Events.InquiryPartnerAddedEvent,
            OnInquiryPartnerAddedNotifyHandler>();

        services.AddDomainEventHandler<Domain.Events.InquiryPartnerQuestionSubmittedEvent,
            OnInquiryPartnerQuestionSubmittedNotifyHandler>();

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
