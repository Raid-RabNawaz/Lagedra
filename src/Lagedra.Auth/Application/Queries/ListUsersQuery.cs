using Lagedra.Auth.Application.DTOs;
using Lagedra.Auth.Domain;
using Lagedra.Auth.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Auth.Application.Queries;

public sealed record ListUsersQuery(int Page = 1, int PageSize = 50) : IRequest<Result<IReadOnlyList<UserProfileDto>>>;

public sealed class ListUsersQueryHandler(AuthDbContext dbContext)
    : IRequestHandler<ListUsersQuery, Result<IReadOnlyList<UserProfileDto>>>
{
    public async Task<Result<IReadOnlyList<UserProfileDto>>> Handle(
        ListUsersQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var users = await dbContext.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var dtos = users.Select(GetCurrentUserQueryHandler.MapToDto).ToList();

        return Result<IReadOnlyList<UserProfileDto>>.Success(dtos);
    }
}
