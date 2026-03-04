using Lagedra.Modules.IdentityAndVerification.Application.DTOs;
using Lagedra.Modules.IdentityAndVerification.Domain.Entities;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.Queries;

public sealed record GetFraudFlagsQuery(Guid UserId) : IRequest<Result<IReadOnlyList<FraudFlagDto>>>;

public sealed class GetFraudFlagsQueryHandler(IdentityDbContext dbContext)
    : IRequestHandler<GetFraudFlagsQuery, Result<IReadOnlyList<FraudFlagDto>>>
{
    public async Task<Result<IReadOnlyList<FraudFlagDto>>> Handle(
        GetFraudFlagsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var flags = await dbContext.FraudFlags
            .AsNoTracking()
            .Where(f => f.UserId == request.UserId)
            .OrderByDescending(f => f.RaisedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<FraudFlagDto> result = flags
            .Select(MapToDto)
            .ToList();

        return Result<IReadOnlyList<FraudFlagDto>>.Success(result);
    }

    private static FraudFlagDto MapToDto(FraudFlag f) =>
        new(f.Id, f.UserId, f.Reason, f.Source,
            f.RaisedAt, f.SlaDeadline, f.ResolvedAt, f.IsEscalated);
}
