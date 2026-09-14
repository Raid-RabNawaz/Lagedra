namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Count of a tenant's completed (closed) stays. Implemented by ActivationAndBilling.
/// </summary>
public interface ICompletedStayCountProvider
{
    Task<int> GetClosedStayCountAsync(Guid tenantUserId, CancellationToken ct = default);
}
