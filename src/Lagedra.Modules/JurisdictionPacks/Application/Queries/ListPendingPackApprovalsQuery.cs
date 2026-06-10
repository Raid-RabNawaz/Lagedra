using Lagedra.Modules.JurisdictionPacks.Application.DTOs;
using Lagedra.Modules.JurisdictionPacks.Domain.Enums;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.JurisdictionPacks.Application.Queries;

public sealed record PendingPackApprovalDto(
    Guid PackId,
    string JurisdictionCode,
    Guid VersionId,
    int VersionNumber,
    DateTime? EffectiveDate,
    Guid? ApprovedBy,
    Guid? SecondApproverId);

public sealed record ListPendingPackApprovalsQuery : IRequest<Result<IReadOnlyList<PendingPackApprovalDto>>>;

public sealed class ListPendingPackApprovalsQueryHandler(JurisdictionDbContext dbContext)
    : IRequestHandler<ListPendingPackApprovalsQuery, Result<IReadOnlyList<PendingPackApprovalDto>>>
{
    public async Task<Result<IReadOnlyList<PendingPackApprovalDto>>> Handle(
        ListPendingPackApprovalsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pending = await (
            from v in dbContext.PackVersions.AsNoTracking()
            join p in dbContext.JurisdictionPacks.AsNoTracking() on v.PackId equals p.Id
            where v.Status == PackVersionStatus.PendingApproval
            orderby p.JurisdictionCode.Code, v.VersionNumber descending
            select new PendingPackApprovalDto(
                p.Id,
                p.JurisdictionCode.Code,
                v.Id,
                v.VersionNumber,
                v.EffectiveDate,
                v.ApprovedBy,
                v.SecondApproverId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<PendingPackApprovalDto>>.Success(pending);
    }
}
