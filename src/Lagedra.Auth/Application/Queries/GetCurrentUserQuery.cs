using Lagedra.Auth.Application.DTOs;
using Lagedra.Auth.Application.Errors;
using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Lagedra.Auth.Application.Queries;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<Result<UserProfileDto>>;

public sealed class GetCurrentUserQueryHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<GetCurrentUserQuery, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await userManager.FindByIdAsync(request.UserId.ToString()).ConfigureAwait(true);
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        return Result<UserProfileDto>.Success(new UserProfileDto(
            UserId: user.Id,
            Email: user.Email!,
            Role: user.Role,
            IsActive: user.IsActive,
            CreatedAt: user.CreatedAt,
            LastLoginAt: user.LastLoginAt));
    }
}
