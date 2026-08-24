using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Application.Services;
using Lagedra.Modules.ActivationAndBilling.Domain.ValueObjects;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record ConsentOwnerTenancyCommand(
    Guid ApplicationId,
    Guid CallerUserId,
    bool ConsentGiven = true,
    string? ConsentVersion = null,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<DealApplicationDto>>;

public sealed class ConsentOwnerTenancyCommandHandler(
    BillingDbContext dbContext,
    IAuditTrailWriter auditTrail)
    : IRequestHandler<ConsentOwnerTenancyCommand, Result<DealApplicationDto>>
{
    private static readonly Error NotFound = new("Application.NotFound", "Application not found.");
    private static readonly Error Forbidden = new(
        "Application.OwnerConsentForbidden",
        "Only the named home owner can consent to this tenancy.");
    private static readonly Error ConsentRequired = new(
        "Application.OwnerConsentNotGiven",
        "You must agree to the owner consent terms to authorize this tenancy.");
    private static readonly Error NotRequired = new(
        "Application.OwnerConsentNotRequired",
        "This booking does not require home-owner consent.");
    private static readonly Error NotPending = new(
        "Application.NotPending",
        "This request is no longer waiting for owner consent.");

    public async Task<Result<DealApplicationDto>> Handle(
        ConsentOwnerTenancyCommand request,
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

        if (!application.OwnerConsentRequired || application.HomeOwnerUserId is null)
        {
            return Result<DealApplicationDto>.Failure(NotRequired);
        }

        if (application.HomeOwnerUserId != request.CallerUserId)
        {
            return Result<DealApplicationDto>.Failure(Forbidden);
        }

        if (application.OwnerTenancyConsentGiven)
        {
            return Result<DealApplicationDto>.Success(DealApplicationDtoMapper.ToDto(application));
        }

        if (application.Status != Domain.Enums.DealApplicationStatus.Pending
            || application.OwnerTenancyConsentDeclined)
        {
            return Result<DealApplicationDto>.Failure(NotPending);
        }

        if (!request.ConsentGiven)
        {
            return Result<DealApplicationDto>.Failure(ConsentRequired);
        }

        try
        {
            application.RecordOwnerConsent(
                request.CallerUserId,
                new TruthSurfaceConsentInput(
                    true,
                    request.ConsentVersion ?? OwnerTenancyConsent.CurrentVersion,
                    request.IpAddress,
                    request.UserAgent));
        }
        catch (InvalidOperationException ex)
        {
            return Result<DealApplicationDto>.Failure(
                new Error("Application.OwnerConsentFailed", ex.Message));
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await auditTrail.RecordAsync(
            request.CallerUserId,
            "booking.owner_consent_given",
            "Application",
            application.Id.ToString(),
            $"{{\"consentVersion\":\"{application.OwnerConsentVersion}\"}}",
            request.IpAddress,
            cancellationToken).ConfigureAwait(false);

        return Result<DealApplicationDto>.Success(DealApplicationDtoMapper.ToDto(application));
    }
}
