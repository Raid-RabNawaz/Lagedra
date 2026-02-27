using Lagedra.Auth.Application.DTOs;
using Lagedra.Auth.Application.Errors;
using Lagedra.Auth.Application.Queries;
using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Lagedra.Auth.Application.Commands;

public sealed record UpdateProfileCommand(
    Guid UserId,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? PhoneNumber,
    string? Bio,
    Uri? ProfilePhotoUrl,
    string? City,
    string? State,
    string? Country,
    string? Languages,
    string? Occupation,
    DateOnly? DateOfBirth,
    string? EmergencyContactName,
    string? EmergencyContactPhone) : IRequest<Result<UserProfileDto>>;

public sealed class UpdateProfileCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<UpdateProfileCommand, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userManager.FindByIdAsync(request.UserId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.DisplayName = request.DisplayName;
        user.PhoneNumber = request.PhoneNumber;
        user.Bio = request.Bio;
        user.ProfilePhotoUrl = request.ProfilePhotoUrl;
        user.City = request.City;
        user.State = request.State;
        user.Country = request.Country;
        user.Languages = request.Languages;
        user.Occupation = request.Occupation;
        user.DateOfBirth = request.DateOfBirth;
        user.EmergencyContactName = request.EmergencyContactName;
        user.EmergencyContactPhone = request.EmergencyContactPhone;

        var result = await userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return AuthErrors.IdentityError(result.Errors.First().Description);
        }

        return Result<UserProfileDto>.Success(GetCurrentUserQueryHandler.MapToDto(user));
    }
}
