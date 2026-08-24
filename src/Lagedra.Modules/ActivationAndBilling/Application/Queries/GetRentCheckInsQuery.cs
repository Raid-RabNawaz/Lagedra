using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Queries;

/// <summary>
/// Lists a deal's monthly rent check-ins. Visible to both parties (the host
/// answers them; the tenant can see what the host reported) and admins.
/// </summary>
public sealed record GetRentCheckInsQuery(
    Guid DealId,
    Guid CallerUserId,
    bool IsAdmin = false) : IRequest<Result<IReadOnlyList<RentCheckInDto>>>;

public sealed class GetRentCheckInsQueryHandler(BillingDbContext dbContext)
    : IRequestHandler<GetRentCheckInsQuery, Result<IReadOnlyList<RentCheckInDto>>>
{
    public async Task<Result<IReadOnlyList<RentCheckInDto>>> Handle(
        GetRentCheckInsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var application = await dbContext.DealApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (application is null)
        {
            return Result<IReadOnlyList<RentCheckInDto>>.Failure(
                new Error("RentCheckIn.DealNotFound", "No deal found for this id."));
        }

        var isParty = application.LandlordUserId == request.CallerUserId
            || application.TenantUserId == request.CallerUserId;
        if (!isParty && !request.IsAdmin)
        {
            return Result<IReadOnlyList<RentCheckInDto>>.Failure(
                new Error("RentCheckIn.Forbidden", "You do not have access to this deal's rent check-ins."));
        }

        var checkIns = await dbContext.RentCheckIns
            .AsNoTracking()
            .Where(r => r.DealId == request.DealId)
            .OrderByDescending(r => r.PeriodStart)
            .Select(r => new RentCheckInDto(
                r.Id, r.DealId, r.PeriodStart, r.PeriodEnd, r.Status, r.RespondedAt, r.Note))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<RentCheckInDto>>.Success(checkIns);
    }
}
