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

        return Result<UserProfileDto>.Success(MapToDto(user));
    }

    internal static UserProfileDto MapToDto(ApplicationUser user) =>
        new(
            UserId: user.Id,
            Email: user.Email!,
            Role: user.Role,
            IsActive: user.IsActive,
            FirstName: user.FirstName,
            LastName: user.LastName,
            DisplayName: user.DisplayName,
            PhoneNumber: user.PhoneNumber,
            Bio: user.Bio,
            ProfilePhotoUrl: user.ProfilePhotoUrl,
            City: user.City,
            State: user.State,
            Country: user.Country,
            Languages: user.Languages,
            Occupation: user.Occupation,
            DateOfBirth: user.DateOfBirth,
            EmergencyContactName: user.EmergencyContactName,
            EmergencyContactPhone: user.EmergencyContactPhone,
            IsGovernmentIdVerified: user.IsGovernmentIdVerified,
            IsPhoneVerified: user.IsPhoneVerified,
            ResponseRatePercent: user.ResponseRatePercent,
            ResponseTimeMinutes: user.ResponseTimeMinutes,
            MemberSince: user.CreatedAt,
            LastLoginAt: user.LastLoginAt);
}
