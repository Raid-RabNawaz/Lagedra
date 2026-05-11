using Lagedra.SharedKernel.Results;

namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Cross-module hook for creating an invited user account on behalf of another actor.
/// Implemented by the Auth module (<c>Lagedra.Auth</c>); consumed by the PartnerNetwork
/// module (Phase 18.4) when a partner creates an account for one of their guests /
/// employees / clients.
///
/// The implementation MUST:
///   1. Create an <c>ApplicationUser</c> with role <c>Member</c>, a random unguessable
///      password, and <c>EmailConfirmed = true</c> (the partner-mediated invitation
///      itself stands in for email confirmation).
///   2. Generate a long-lived (7-day) password-set token via
///      <c>UserManager.GeneratePasswordResetTokenAsync</c>.
///   3. Send a <c>PartnerGuestInvitation</c> email containing the set-password link.
///   4. Return the new user's id and the absolute set-password URL.
///
/// Idempotency: if the email already maps to an existing user, the implementation MUST
/// return <c>Identity.EmailAlreadyExists</c> with the existing user's id surfaced
/// in <see cref="InvitedUserDto.UserId"/>; the caller (PartnerNetwork) treats this as
/// "use this existing user" rather than failing the invite flow.
/// </summary>
public interface IIdentityInvitationService
{
    Task<Result<InvitedUserDto>> CreateOrFindInvitedUserAsync(
        InvitedUserRequest request,
        CancellationToken ct = default);
}

public sealed record InvitedUserRequest(
    string Email,
    string FullName,
    Guid InvitedByUserId,
    string InvitingOrganizationName);

public sealed record InvitedUserDto(
    Guid UserId,
    string Email,
    bool WasJustCreated,
    Uri? SetPasswordUrl,
    DateTime? TokenExpiresAt);
