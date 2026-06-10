using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record RejectDealApplicationCommand(
    Guid ApplicationId,
    Guid CallerUserId) : IRequest<Result<DealApplicationDto>>;

public sealed class RejectDealApplicationCommandHandler(
    BillingDbContext dbContext)
    : IRequestHandler<RejectDealApplicationCommand, Result<DealApplicationDto>>
{
    private static readonly Error Forbidden = new("Application.Forbidden", "You do not own the listing for this application.");

    public async Task<Result<DealApplicationDto>> Handle(
        RejectDealApplicationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var application = await dbContext.DealApplications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken)
            .ConfigureAwait(false);

        if (application is null)
        {
            return Result<DealApplicationDto>.Failure(
                new Error("Application.NotFound", "Application not found."));
        }

        if (application.LandlordUserId != request.CallerUserId)
        {
            return Result<DealApplicationDto>.Failure(Forbidden);
        }

        application.Reject();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<DealApplicationDto>.Success(MapToDto(application));
    }

    private static DealApplicationDto MapToDto(DealApplication a) =>
        new(a.Id, a.ListingId, a.TenantUserId, a.LandlordUserId,
            a.Status, a.DealId, a.SubmittedAt, a.DecidedAt,
            a.RequestedCheckIn, a.RequestedCheckOut, a.StayDurationDays,
            a.DepositAmountCents, a.InsuranceFeeCents, a.FirstMonthRentCents,
            a.PartnerOrganizationId, a.IsPartnerReferred, a.JurisdictionWarning, a.Source,
            a.GuestCount, a.Message);
}
