namespace Lagedra.SharedKernel.Insurance;

/// <summary>
/// Provides insurance status for a deal. Used for listing verification badges
/// when viewing in deal context. Implemented by InsuranceIntegration module.
/// </summary>
public interface IInsuranceStatusProvider
{
    Task<bool> IsActiveAsync(Guid dealId, CancellationToken ct = default);
}
