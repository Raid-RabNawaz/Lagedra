namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Party fields needed to fill a lease agreement (host or tenant).
/// </summary>
public sealed record LeasePartyProfileDto(
    Guid UserId,
    string FullName,
    string? Email,
    string? Phone,
    string? MailingAddress,
    string? NoticeAddress,
    string? BrokerName,
    string? BrokerDreLicense,
    string? BrokerScopeNotes,
    string? FirstName = null,
    string? LastName = null,
    string? CompanyName = null);

public interface ILeasePartyProfileProvider
{
    Task<LeasePartyProfileDto?> GetAsync(Guid userId, CancellationToken ct = default);
}
