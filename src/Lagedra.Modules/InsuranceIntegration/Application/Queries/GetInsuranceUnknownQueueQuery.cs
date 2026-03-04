using Lagedra.Modules.InsuranceIntegration.Application.DTOs;
using Lagedra.Modules.InsuranceIntegration.Domain.Enums;
using Lagedra.Modules.InsuranceIntegration.Domain.Policies;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.InsuranceIntegration.Application.Queries;

public sealed record GetInsuranceUnknownQueueQuery : IRequest<Result<IReadOnlyList<InsuranceQueueItemDto>>>;

public sealed class GetInsuranceUnknownQueueQueryHandler(
    InsuranceDbContext dbContext)
    : IRequestHandler<GetInsuranceUnknownQueueQuery, Result<IReadOnlyList<InsuranceQueueItemDto>>>
{
    public async Task<Result<IReadOnlyList<InsuranceQueueItemDto>>> Handle(
        GetInsuranceUnknownQueueQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var utcNow = DateTime.UtcNow;

        var records = await dbContext.PolicyRecords
            .AsNoTracking()
            .Where(r => r.State == InsuranceState.Unknown && r.UnknownSince != null)
            .OrderBy(r => r.UnknownSince)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = records.Select(r =>
        {
            var elapsed = (utcNow - r.UnknownSince!.Value).TotalHours;
            var remaining = Math.Max(0, UnknownGraceWindowPolicy.GraceHours - elapsed);
            return new InsuranceQueueItemDto(r.Id, r.DealId, r.TenantUserId, r.UnknownSince.Value, remaining);
        }).ToList();

        return Result<IReadOnlyList<InsuranceQueueItemDto>>.Success(items);
    }
}
