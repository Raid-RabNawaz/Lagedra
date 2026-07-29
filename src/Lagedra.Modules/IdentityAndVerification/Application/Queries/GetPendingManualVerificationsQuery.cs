using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.Queries;

public sealed record ManualVerificationItemDto(
    Guid ProfileId,
    Guid UserId,
    string? Email,
    string? FirstName,
    string? LastName,
    DateTime SubmittedAt,
    double HoursRemaining);

public sealed record GetPendingManualVerificationsQuery
    : IRequest<Result<IReadOnlyList<ManualVerificationItemDto>>>;

public sealed class GetPendingManualVerificationsQueryHandler(
    IdentityDbContext dbContext,
    IUserEmailResolver emailResolver)
    : IRequestHandler<GetPendingManualVerificationsQuery, Result<IReadOnlyList<ManualVerificationItemDto>>>
{
    private const double SlaHours = 24;

    public async Task<Result<IReadOnlyList<ManualVerificationItemDto>>> Handle(
        GetPendingManualVerificationsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var utcNow = DateTime.UtcNow;

        var profiles = await dbContext.IdentityProfiles
            .AsNoTracking()
            .Where(p => p.Status == Domain.Enums.VerificationStatus.ManualReviewRequired)
            .OrderBy(p => p.UpdatedAt)
            .Select(p => new
            {
                p.Id,
                p.UserId,
                p.FirstName,
                p.LastName,
                p.UpdatedAt,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = new List<ManualVerificationItemDto>(profiles.Count);
        foreach (var p in profiles)
        {
            var email = await emailResolver.GetEmailAsync(p.UserId, cancellationToken)
                .ConfigureAwait(false);
            var elapsed = (utcNow - p.UpdatedAt).TotalHours;
            var remaining = Math.Max(0, SlaHours - elapsed);
            items.Add(new ManualVerificationItemDto(
                p.Id, p.UserId, email, p.FirstName, p.LastName, p.UpdatedAt, remaining));
        }

        return Result<IReadOnlyList<ManualVerificationItemDto>>.Success(items);
    }
}
