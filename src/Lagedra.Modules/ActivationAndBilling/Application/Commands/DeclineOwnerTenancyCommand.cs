using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record DeclineOwnerTenancyCommand(
    Guid ApplicationId,
    Guid CallerUserId) : IRequest<Result<DealApplicationDto>>;

public sealed class DeclineOwnerTenancyCommandHandler(
    BillingDbContext dbContext,
    IAuditTrailWriter auditTrail)
    : IRequestHandler<DeclineOwnerTenancyCommand, Result<DealApplicationDto>>
{
    private static readonly Error NotFound = new("Application.NotFound", "Application not found.");
    private static readonly Error Forbidden = new(
        "Application.OwnerConsentForbidden",
        "Only the named home owner can decline this tenancy.");
    private static readonly Error NotRequired = new(
        "Application.OwnerConsentNotRequired",
        "This booking does not require home-owner consent.");
    private static readonly Error NotPending = new(
        "Application.NotPending",
        "This request is no longer waiting for owner consent.");

    public async Task<Result<DealApplicationDto>> Handle(
        DeclineOwnerTenancyCommand request,
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

        if (application.Status != Domain.Enums.DealApplicationStatus.Pending
            || application.OwnerTenancyConsentGiven
            || application.OwnerTenancyConsentDeclined)
        {
            return Result<DealApplicationDto>.Failure(NotPending);
        }

        try
        {
            application.DeclineOwnerConsent(request.CallerUserId);
        }
        catch (InvalidOperationException ex)
        {
            return Result<DealApplicationDto>.Failure(
                new Error("Application.OwnerDeclineFailed", ex.Message));
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await auditTrail.RecordAsync(
            request.CallerUserId,
            "booking.owner_consent_declined",
            "Application",
            application.Id.ToString(),
            null,
            ct: cancellationToken).ConfigureAwait(false);

        return Result<DealApplicationDto>.Success(DealApplicationDtoMapper.ToDto(application));
    }
}
