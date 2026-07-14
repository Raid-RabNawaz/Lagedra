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
    string? EmergencyContactPhone,
    string? MailingStreet = null,
    string? MailingCity = null,
    string? MailingState = null,
    string? MailingZip = null,
    string? MailingCountry = null,
    bool? NoticeAddressSameAsMailing = null,
    string? NoticeStreet = null,
    string? NoticeCity = null,
    string? NoticeState = null,
    string? NoticeZip = null,
    string? NoticeCountry = null,
    string? BrokerName = null,
    string? BrokerDreLicense = null,
    string? BrokerScopeNotes = null) : IRequest<Result<UserProfileDto>>;

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

        var incomingPhone = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : request.PhoneNumber.Trim();
        var previousPhone = string.IsNullOrWhiteSpace(user.PhoneNumber)
            ? null
            : user.PhoneNumber.Trim();

        if (!string.Equals(previousPhone, incomingPhone, StringComparison.Ordinal))
        {
            user.PhoneNumber = incomingPhone;
            user.IsPhoneVerified = false;
            user.PhoneNumberConfirmed = false;
            user.PhoneVerificationCodeHash = null;
            user.PhoneVerificationExpiresAt = null;
            user.PhoneVerificationSentAt = null;
            user.PhoneVerificationWindowStartedAt = null;
            user.PhoneVerificationSendCount = 0;
        }

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
        user.MailingStreet = request.MailingStreet;
        user.MailingCity = request.MailingCity;
        user.MailingState = request.MailingState;
        user.MailingZip = request.MailingZip;
        user.MailingCountry = request.MailingCountry;
        if (request.NoticeAddressSameAsMailing.HasValue)
        {
            user.NoticeAddressSameAsMailing = request.NoticeAddressSameAsMailing.Value;
        }

        user.NoticeStreet = request.NoticeStreet;
        user.NoticeCity = request.NoticeCity;
        user.NoticeState = request.NoticeState;
        user.NoticeZip = request.NoticeZip;
        user.NoticeCountry = request.NoticeCountry;
        user.BrokerName = request.BrokerName;
        user.BrokerDreLicense = request.BrokerDreLicense;
        user.BrokerScopeNotes = request.BrokerScopeNotes;

        var result = await userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return AuthErrors.IdentityError(result.Errors.First().Description);
        }

        return Result<UserProfileDto>.Success(GetCurrentUserQueryHandler.MapToDto(user));
    }
}
