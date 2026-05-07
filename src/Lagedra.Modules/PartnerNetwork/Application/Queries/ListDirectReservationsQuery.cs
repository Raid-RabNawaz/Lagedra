using Lagedra.Modules.PartnerNetwork.Application.Authorization;
using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Queries;

public sealed record ListDirectReservationsQuery(
    Guid OrganizationId,
    Guid CallerUserId,
    bool CallerIsPlatformAdmin,
    DirectReservationStatusFilter StatusFilter,
    int Skip,
    int Take) : IRequest<Result<IReadOnlyList<DirectReservationDto>>>;

public enum DirectReservationStatusFilter
{
    All,
    Pending,
    Linked
}

public sealed class ListDirectReservationsQueryHandler(
    PartnerDbContext dbContext,
    IPartnerAccessService accessService)
    : IRequestHandler<ListDirectReservationsQuery, Result<IReadOnlyList<DirectReservationDto>>>
{
    public async Task<Result<IReadOnlyList<DirectReservationDto>>> Handle(
        ListDirectReservationsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authzResult = await accessService.RequireMemberAsync(
            request.CallerUserId,
            request.OrganizationId,
            request.CallerIsPlatformAdmin,
            cancellationToken).ConfigureAwait(false);

        if (authzResult.IsFailure)
        {
            return Result<IReadOnlyList<DirectReservationDto>>.Failure(authzResult.Error);
        }

        var query = dbContext.DirectReservations
            .AsNoTracking()
            .Where(r => r.OrganizationId == request.OrganizationId);

        query = request.StatusFilter switch
        {
            DirectReservationStatusFilter.Pending => query.Where(r => r.DealApplicationId == null),
            DirectReservationStatusFilter.Linked => query.Where(r => r.DealApplicationId != null),
            _ => query
        };

        var skip = Math.Max(0, request.Skip);
        var take = Math.Clamp(request.Take <= 0 ? 50 : request.Take, 1, 200);

        var reservations = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(r => new DirectReservationDto(
                r.Id, r.OrganizationId, r.GuestName, r.GuestEmail,
                r.ListingId, r.DealApplicationId, r.ReservedByUserId, r.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<DirectReservationDto>>.Success(reservations);
    }
}
