using Lagedra.Modules.IdentityAndVerification.Application.DTOs;
using Lagedra.Modules.IdentityAndVerification.Domain.Aggregates;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.Queries;

public sealed record GetVerificationStatusQuery(Guid UserId) : IRequest<Result<VerificationStatusDto>>;

public sealed class GetVerificationStatusQueryHandler(IdentityDbContext dbContext)
    : IRequestHandler<GetVerificationStatusQuery, Result<VerificationStatusDto>>
{
    public async Task<Result<VerificationStatusDto>> Handle(
        GetVerificationStatusQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = await dbContext.IdentityProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
        {
            return Result<VerificationStatusDto>.Failure(
                new Error("Identity.NotFound", "Identity profile not found."));
        }

        return Result<VerificationStatusDto>.Success(MapToDto(profile));
    }

    private static VerificationStatusDto MapToDto(IdentityProfile p) =>
        new(p.Id, p.UserId, p.Status, p.VerificationClass,
            p.FirstName, p.LastName, p.DateOfBirth, p.CreatedAt);
}
