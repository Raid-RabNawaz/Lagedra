namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Reverse-direction companion of <see cref="IUserEmailResolver"/>: resolves a user id
/// from an email address. Implemented in <c>Lagedra.Auth</c>; consumed by the partner
/// direct-reservation flow (Phase 18.7) so the PartnerNetwork module can convert a
/// raw <c>guestEmail</c> into a real <c>TenantUserId</c> for <c>DealApplication.Submit</c>
/// without taking a hard dependency on Auth's <c>UserManager</c>.
/// </summary>
public interface IUserLookupService
{
    Task<Guid?> FindUserIdByEmailAsync(string email, CancellationToken ct = default);

    Task<UserAccountLookupDto?> FindAccountByEmailAsync(string email, CancellationToken ct = default);

    Task<UserAccountLookupDto?> FindAccountByIdAsync(Guid userId, CancellationToken ct = default);
}
