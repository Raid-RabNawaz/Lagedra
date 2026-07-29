using Lagedra.Compliance.Application.EventHandlers;
using Lagedra.Compliance.Domain.Events;
using Lagedra.Compliance.Infrastructure.Persistence;
using Lagedra.Compliance.Infrastructure.Repositories;
using Lagedra.Compliance.Infrastructure.Services;
using Lagedra.Infrastructure.Eventing;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lagedra.Compliance;

public static class ComplianceModuleRegistration
{
    public static IServiceCollection AddCompliance(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<ComplianceDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<ComplianceDbContext>();

        services.AddScoped<ViolationRepository>();
        services.AddScoped<TrustLedgerRepository>();
        services.AddScoped<IUserViolationCountProvider, UserViolationCountProvider>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ComplianceModuleRegistration).Assembly));

        services.AddDomainEventHandler<ViolationCreatedEvent, OnViolationCreatedNotify>();
        services.AddDomainEventHandler<ViolationResolvedEvent, OnViolationResolvedNotify>();
        services.AddDomainEventHandler<ViolationEscalatedEvent, OnViolationEscalatedNotify>();

        services.AddDomainEventHandler<SharedKernel.Integration.Events.InsuranceStatusChangedEvent, OnInsuranceStatusChangedCreateSignalHandler>();
        services.AddDomainEventHandler<SharedKernel.Integration.Events.BillingStoppedEvent, OnBillingStoppedCreateSignalHandler>();
        services.AddDomainEventHandler<SharedKernel.Integration.Events.DealCompletedEvent, OnDealCompletedCreateSignalHandler>();
        services.AddDomainEventHandler<SharedKernel.Integration.Events.PositiveReviewEarnedEvent, OnPositiveReviewEarnedCreateSignalHandler>();
        services.AddDomainEventHandler<SharedKernel.Integration.Events.ReviewConcernRaisedEvent, OnReviewConcernRaisedCreateSignalHandler>();

        // Trust ledger recorders — every event that moves a user's trust level
        // must land in the ledger so the user-facing record is complete.
        services.AddDomainEventHandler<SharedKernel.Integration.Events.EmailVerifiedEvent, OnEmailVerifiedRecordLedgerEntryHandler>();
        services.AddDomainEventHandler<SharedKernel.Integration.Events.PhoneVerifiedEvent, OnPhoneVerifiedRecordLedgerEntryHandler>();
        services.AddDomainEventHandler<SharedKernel.Integration.Events.IdentityVerifiedEvent, OnIdentityVerifiedRecordLedgerEntryHandler>();
        services.AddDomainEventHandler<SharedKernel.Integration.Events.BackgroundCheckReceivedEvent, OnBackgroundCheckReceivedRecordLedgerEntryHandler>();
        services.AddDomainEventHandler<SharedKernel.Integration.Events.PartnerEndorsementApprovedEvent, OnPartnerEndorsementApprovedRecordLedgerEntryHandler>();
        services.AddDomainEventHandler<SharedKernel.Integration.Events.PartnerEndorsementRevokedEvent, OnPartnerEndorsementRevokedRecordLedgerEntryHandler>();
        services.AddDomainEventHandler<SharedKernel.Integration.Events.PartnerEndorsementExpiredEvent, OnPartnerEndorsementExpiredRecordLedgerEntryHandler>();
        services.AddDomainEventHandler<SharedKernel.Integration.Events.BookingCancelledEvent, OnBookingCancelledRecordLedgerEntryHandler>();
        services.AddDomainEventHandler<SharedKernel.Integration.Events.ArbitrationRulingIssuedEvent, OnArbitrationRulingRecordLedgerEntryHandler>();

        return services;
    }
}
