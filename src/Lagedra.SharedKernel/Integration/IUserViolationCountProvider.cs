namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Returns the count of active (open / escalated) violations attributed to a user.
/// Implemented by the Compliance module.
/// </summary>
public interface IUserViolationCountProvider
{
    Task<int> GetActiveViolationCountAsync(Guid userId, CancellationToken ct = default);
}
