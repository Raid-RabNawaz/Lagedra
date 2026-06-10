namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Lists platform arbitrators for case assignment. Implemented in Auth.
/// </summary>
public interface IArbitratorPanelProvider
{
    Task<IReadOnlyList<ArbitratorPanelMemberDto>> GetPanelMembersAsync(CancellationToken ct = default);
}

public sealed record ArbitratorPanelMemberDto(
    Guid UserId,
    string Email,
    string? DisplayName);
