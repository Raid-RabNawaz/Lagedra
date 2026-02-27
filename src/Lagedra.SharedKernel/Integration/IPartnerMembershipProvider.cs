namespace Lagedra.SharedKernel.Integration;

public interface IPartnerMembershipProvider
{
    Task<Guid?> GetPartnerOrganizationIdAsync(Guid userId, CancellationToken ct = default);
}
