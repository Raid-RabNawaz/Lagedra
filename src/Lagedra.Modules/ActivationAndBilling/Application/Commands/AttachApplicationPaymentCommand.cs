using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Application.Services;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Domain.ValueObjects;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

/// <summary>
/// Tenant completes payment readiness on a Pending partner-direct request:
/// attaches a Stripe payment method and Truth Surface consent so the host can approve.
/// For PartnerOrganization-payer requests, only tenant Truth Surface consent is required
/// (the partner already attached the card at reservation create).
/// </summary>
public sealed record AttachApplicationPaymentCommand(
    Guid ApplicationId,
    Guid CallerUserId,
    string? StripePaymentMethodId,
    bool TruthSurfaceConsentGiven,
    string? ConsentVersion = null,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<DealApplicationDto>>;

public sealed class AttachApplicationPaymentCommandHandler(
    BillingDbContext dbContext)
    : IRequestHandler<AttachApplicationPaymentCommand, Result<DealApplicationDto>>
{
    private static readonly Error NotFound =
        new("Application.NotFound", "Application not found.");
    private static readonly Error Forbidden =
        new("Application.Forbidden", "Only the tenant on this request can complete payment.");
    private static readonly Error NotPending =
        new("Application.NotPending", "Payment can only be attached while the request is pending.");
    private static readonly Error ConsentRequired =
        new("Application.TenantConsentRequired",
            "You must agree to the Truth Surface terms to complete this request.");
    private static readonly Error PaymentRequired =
        new("Application.PaymentRequired",
            "A payment method is required when you are paying for this booking.");

    public async Task<Result<DealApplicationDto>> Handle(
        AttachApplicationPaymentCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var application = await dbContext.DealApplications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken)
            .ConfigureAwait(false);

        if (application is null)
        {
            return Result<DealApplicationDto>.Failure(NotFound);
        }

        if (application.TenantUserId != request.CallerUserId)
        {
            return Result<DealApplicationDto>.Failure(Forbidden);
        }

        if (application.Status != DealApplicationStatus.Pending)
        {
            return Result<DealApplicationDto>.Failure(NotPending);
        }

        if (!request.TruthSurfaceConsentGiven)
        {
            return Result<DealApplicationDto>.Failure(ConsentRequired);
        }

        var consent = new TruthSurfaceConsentInput(
            true,
            request.ConsentVersion ?? BookingConsent.CurrentVersion,
            request.IpAddress,
            request.UserAgent);

        if (application.PayerType == ApplicationPayerType.Tenant)
        {
            if (string.IsNullOrWhiteSpace(request.StripePaymentMethodId)
                && string.IsNullOrWhiteSpace(application.StripePaymentMethodId))
            {
                return Result<DealApplicationDto>.Failure(PaymentRequired);
            }

            var pm = request.StripePaymentMethodId ?? application.StripePaymentMethodId!;
            application.AttachPaymentMethod(pm, consent);
        }
        else
        {
            // Partner already attached the card; tenant only needs occupancy consent.
            if (!string.IsNullOrWhiteSpace(request.StripePaymentMethodId))
            {
                // Ignore tenant PM when partner pays — keep partner's card.
            }

            if (!application.TenantTruthSurfaceConsentGiven)
            {
                application.RecordTenantConsent(consent);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<DealApplicationDto>.Success(DealApplicationDtoMapper.ToDto(application));
    }
}
