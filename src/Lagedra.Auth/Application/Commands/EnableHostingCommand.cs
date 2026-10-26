using Lagedra.Auth.Application.DTOs;
using Lagedra.Auth.Application.Errors;
using Lagedra.Auth.Application.Queries;
using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Lagedra.Auth.Application.Commands;

/// <summary>
/// Turns a guest account into one that can also host. Collects the details a
/// listing needs that signup does not ask for, then unlocks the travelling /
/// hosting switch.
/// </summary>
public sealed record EnableHostingCommand(
    Guid UserId,
    string City,
    string Country,
    string Bio,
    HostKind HostKind,
    string? State,
    string? Languages,
    string? Occupation,
    string? CompanyName) : IRequest<Result<UserProfileDto>>;

public sealed class EnableHostingCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<EnableHostingCommand, Result<UserProfileDto>>
{
    public const int MinimumBioLength = 40;

    public async Task<Result<UserProfileDto>> Handle(
        EnableHostingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Enum.IsDefined(request.HostKind))
        {
            return Result<UserProfileDto>.Failure(new Error(
                "Auth.Hosting.InvalidKind",
                "Choose whether you host as an individual or as a property management company."));
        }

        if (string.IsNullOrWhiteSpace(request.City) || string.IsNullOrWhiteSpace(request.Country))
        {
            return Result<UserProfileDto>.Failure(new Error(
                "Auth.Hosting.LocationRequired",
                "Enter the city and country where you host."));
        }

        if (string.IsNullOrWhiteSpace(request.Bio) || request.Bio.Trim().Length < MinimumBioLength)
        {
            return Result<UserProfileDto>.Failure(new Error(
                "Auth.Hosting.BioRequired",
                "Add a short bio (at least 40 characters) so guests know who they're staying with."));
        }

        if (request.HostKind == HostKind.PropertyManagementCompany &&
            string.IsNullOrWhiteSpace(request.CompanyName))
        {
            return Result<UserProfileDto>.Failure(new Error(
                "Auth.Hosting.CompanyNameRequired",
                "Enter the property management company's legal name."));
        }

        var user = await userManager.FindByIdAsync(request.UserId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        if (user.Role != UserRole.Member)
        {
            return Result<UserProfileDto>.Failure(new Error(
                "Auth.Hosting.MembersOnly",
                "Only personal accounts can become hosts."));
        }

        user.City = request.City.Trim();
        user.Country = request.Country.Trim();
        user.State = string.IsNullOrWhiteSpace(request.State) ? user.State : request.State.Trim();
        user.Bio = request.Bio.Trim();
        user.Languages = string.IsNullOrWhiteSpace(request.Languages) ? user.Languages : request.Languages.Trim();
        user.Occupation = string.IsNullOrWhiteSpace(request.Occupation) ? user.Occupation : request.Occupation.Trim();
        user.HostKind = request.HostKind;
        if (request.HostKind == HostKind.PropertyManagementCompany)
        {
            user.CompanyName = request.CompanyName!.Trim();
        }

        user.HostingEnabled = true;

        var update = await userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!update.Succeeded)
        {
            return AuthErrors.IdentityError(update.Errors.First().Description);
        }

        return Result<UserProfileDto>.Success(GetCurrentUserQueryHandler.MapToDto(user));
    }
}
