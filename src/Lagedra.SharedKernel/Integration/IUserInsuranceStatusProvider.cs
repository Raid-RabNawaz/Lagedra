namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Returns the best insurance status across all deals for a user,
/// and resolves deal-to-tenant mappings.
/// Implemented by the InsuranceIntegration module.
/// </summary>
public interface IUserInsuranceStatusProvider
{
    Task<UserInsuranceStatusDto> GetBestStatusForUserAsync(Guid userId, CancellationToken ct = default);
    Task<Guid?> GetTenantUserIdForDealAsync(Guid dealId, CancellationToken ct = default);
}

public sealed record UserInsuranceStatusDto(
    bool HasActivePolicy,
    bool HasInstitutionBackedPolicy);
