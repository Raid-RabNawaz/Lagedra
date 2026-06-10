using Lagedra.Auth.Application.DTOs;
using Lagedra.Auth.Application.Errors;
using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Lagedra.Auth.Application.Queries;

public sealed record GetPublicProfileQuery(Guid UserId) : IRequest<Result<PublicUserProfileDto>>;

public sealed class GetPublicProfileQueryHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<GetPublicProfileQuery, Result<PublicUserProfileDto>>
{
    public async Task<Result<PublicUserProfileDto>> Handle(
        GetPublicProfileQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await userManager.FindByIdAsync(request.UserId.ToString()).ConfigureAwait(true);
        if (user is null || !user.IsActive)
        {
            return AuthErrors.UserNotFound;
        }

        return Result<PublicUserProfileDto>.Success(MapToDto(user));
    }

    private static PublicUserProfileDto MapToDto(ApplicationUser user) =>
        new(
            UserId: user.Id,
            DisplayName: ResolveDisplayName(user),
            FirstName: user.FirstName,
            LastName: user.LastName,
            Bio: user.Bio,
            ProfilePhotoUrl: user.ProfilePhotoUrl,
            City: user.City,
            State: user.State,
            Country: user.Country,
            Languages: user.Languages,
            Occupation: user.Occupation,
            IsGovernmentIdVerified: user.IsGovernmentIdVerified,
            IsPhoneVerified: user.IsPhoneVerified,
            IsEmailVerified: user.EmailConfirmed,
            ResponseRatePercent: user.ResponseRatePercent,
            ResponseTimeMinutes: user.ResponseTimeMinutes,
            MemberSince: user.CreatedAt);

    private static string? ResolveDisplayName(ApplicationUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            return user.DisplayName;
        }

        var combined = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(combined) ? null : combined;
    }
}
