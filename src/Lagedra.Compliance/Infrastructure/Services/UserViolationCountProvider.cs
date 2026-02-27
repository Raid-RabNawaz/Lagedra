using Lagedra.SharedKernel.Integration;

namespace Lagedra.Compliance.Infrastructure.Services;

/// <summary>
/// Stub implementation. Violations are currently tracked by DealId without an explicit
/// TargetUserId column, so accurate per-user counts require a deal-to-user mapping
/// that spans module boundaries. Returns 0 until Violation gains a TargetUserId field.
/// </summary>
public sealed class UserViolationCountProvider : IUserViolationCountProvider
{
    public Task<int> GetActiveViolationCountAsync(Guid userId, CancellationToken ct = default)
    {
        return Task.FromResult(0);
    }
}
