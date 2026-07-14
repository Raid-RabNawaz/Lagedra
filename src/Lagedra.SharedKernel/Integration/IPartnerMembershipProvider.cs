namespace Lagedra.SharedKernel.Integration;

public interface IPartnerMembershipProvider
{
    Task<Guid?> GetPartnerOrganizationIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns user ids of all staff members of the organization (for notifications).
    /// </summary>
    Task<IReadOnlyList<Guid>> GetMemberUserIdsAsync(
        Guid organizationId,
        CancellationToken ct = default);

    /// <summary>
    /// Display name for the partner organization, or null if not found.
    /// </summary>
    Task<string?> GetOrganizationNameAsync(
        Guid organizationId,
        CancellationToken ct = default);
}
