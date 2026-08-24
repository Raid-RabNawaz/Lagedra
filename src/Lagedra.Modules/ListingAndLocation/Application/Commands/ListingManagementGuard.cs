using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

internal static class ListingManagementGuard
{
    public static readonly Error HomeOwnerRequired = new(
        "Listing.HomeOwnerRequired",
        "Select the home owner. Property-manager listings need an owner with a Lagedra account so they can consent on the lease for stays longer than 30 days.");

    public static readonly Error HomeOwnerNotFound = new(
        "Listing.HomeOwnerNotFound",
        "No Lagedra account was found for that home owner. Ask the owner to create an account, then look them up by the email they used to sign up.");

    public static readonly Error HomeOwnerCannotBeSelf = new(
        "Listing.HomeOwnerCannotBeSelf",
        "The home owner must be a different account. If you own the property, choose “I am the home owner” instead.");

    public static async Task<Result<ResolvedListingManagement>> ResolveAsync(
        IUserLookupService userLookup,
        ListingManagerRole managerRole,
        Guid? homeOwnerUserId,
        string? homeOwnerEmail,
        Guid callerUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userLookup);

        if (managerRole != ListingManagerRole.PropertyManager)
        {
            return Result<ResolvedListingManagement>.Success(
                new ResolvedListingManagement(ListingManagerRole.Owner, null, null));
        }

        UserAccountLookupDto? account = null;
        if (homeOwnerUserId is Guid ownerId && ownerId != Guid.Empty)
        {
            account = await userLookup.FindAccountByIdAsync(ownerId, cancellationToken).ConfigureAwait(false);
        }
        else if (!string.IsNullOrWhiteSpace(homeOwnerEmail))
        {
            account = await userLookup
                .FindAccountByEmailAsync(homeOwnerEmail, cancellationToken)
                .ConfigureAwait(false);
        }

        if (account is null)
        {
            // Drafts may be saved without an owner. Submit-for-review still
            // requires one (Listing.SubmitForReview / this guard's error).
            if (homeOwnerUserId is null && string.IsNullOrWhiteSpace(homeOwnerEmail))
            {
                return Result<ResolvedListingManagement>.Success(
                    new ResolvedListingManagement(ListingManagerRole.PropertyManager, null, null));
            }

            return Result<ResolvedListingManagement>.Failure(HomeOwnerNotFound);
        }

        if (account.UserId == callerUserId)
        {
            return Result<ResolvedListingManagement>.Failure(HomeOwnerCannotBeSelf);
        }

        return Result<ResolvedListingManagement>.Success(
            new ResolvedListingManagement(
                ListingManagerRole.PropertyManager,
                account.UserId,
                new ListingHomeOwnerDto(account.UserId, account.DisplayName, account.Email)));
    }
}

internal sealed record ResolvedListingManagement(
    ListingManagerRole ManagerRole,
    Guid? HomeOwnerUserId,
    ListingHomeOwnerDto? HomeOwner);
