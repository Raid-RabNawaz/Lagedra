using Lagedra.Modules.Privacy.Application.DTOs;
using Lagedra.Modules.Privacy.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Privacy.Application.Queries;

public sealed record GetUserConsentsQuery(Guid UserId) : IRequest<Result<IReadOnlyList<ConsentDto>>>;

public sealed class GetUserConsentsQueryHandler(PrivacyDbContext dbContext)
    : IRequestHandler<GetUserConsentsQuery, Result<IReadOnlyList<ConsentDto>>>
{
    public async Task<Result<IReadOnlyList<ConsentDto>>> Handle(
        GetUserConsentsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userConsent = await dbContext.UserConsents
            .AsNoTracking()
            .Include(uc => uc.ConsentRecords)
            .FirstOrDefaultAsync(uc => uc.UserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (userConsent is null)
        {
            return Result<IReadOnlyList<ConsentDto>>.Success(Array.Empty<ConsentDto>());
        }

        var dtos = userConsent.ConsentRecords
            .Select(r => new ConsentDto(r.ConsentType, r.GrantedAt, r.WithdrawnAt, r.IpAddress, r.UserAgent))
            .ToList();

        return Result<IReadOnlyList<ConsentDto>>.Success(dtos);
    }
}
