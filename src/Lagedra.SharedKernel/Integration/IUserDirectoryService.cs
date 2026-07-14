namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Lightweight user directory for cross-module roster / display enrichment.
/// Implemented in <c>Lagedra.Auth</c>.
/// </summary>
public sealed record UserDirectoryEntry(
    Guid UserId,
    string Email,
    string DisplayName);

public interface IUserDirectoryService
{
    Task<IReadOnlyDictionary<Guid, UserDirectoryEntry>> GetEntriesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken ct = default);
}
