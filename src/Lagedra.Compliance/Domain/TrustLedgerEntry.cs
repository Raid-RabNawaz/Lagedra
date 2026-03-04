using Lagedra.SharedKernel.Domain;

namespace Lagedra.Compliance.Domain;

/// <summary>
/// Immutable, append-only trust ledger entry. Once written, it can never
/// be modified or deleted. This is the permanent record of a user's
/// trust-relevant actions across the platform.
/// </summary>
public sealed class TrustLedgerEntry : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public TrustLedgerEntryType EntryType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public string? Description { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public bool IsPublic { get; private set; }

    private TrustLedgerEntry() { }

    public static TrustLedgerEntry Create(
        Guid userId,
        TrustLedgerEntryType entryType,
        Guid? referenceId,
        string? description,
        bool isPublic)
    {
        return new TrustLedgerEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EntryType = entryType,
            ReferenceId = referenceId,
            Description = description,
            OccurredAt = DateTime.UtcNow,
            IsPublic = isPublic
        };
    }
}
