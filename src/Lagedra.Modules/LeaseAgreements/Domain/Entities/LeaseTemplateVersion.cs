using Lagedra.Modules.LeaseAgreements.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.LeaseAgreements.Domain.Entities;

public sealed class LeaseTemplateVersion : Entity<Guid>
{
    public Guid TemplateId { get; private set; }
    public int VersionNumber { get; private set; }
    public LeaseTemplateVersionStatus Status { get; private set; }
    public DateTime? EffectiveDate { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public Guid? SecondApproverId { get; private set; }

    /// <summary>HTML body with {{placeholder}} tokens.</summary>
    public string BodyHtml { get; private set; } = string.Empty;

    private LeaseTemplateVersion() { }

    internal static LeaseTemplateVersion Create(Guid templateId, int versionNumber, string? bodyHtml = null)
    {
        return new LeaseTemplateVersion
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            VersionNumber = versionNumber,
            Status = LeaseTemplateVersionStatus.Draft,
            BodyHtml = bodyHtml ?? string.Empty
        };
    }

    public void UpdateDraft(string bodyHtml, DateTime? effectiveDate)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(bodyHtml);
        BodyHtml = bodyHtml;
        if (effectiveDate.HasValue)
        {
            EffectiveDate = effectiveDate;
        }
    }

    public void SetEffectiveDate(DateTime effectiveDate)
    {
        EnsureDraft();
        EffectiveDate = effectiveDate;
    }

    public void RequestApproval()
    {
        EnsureDraft();

        if (EffectiveDate is null)
        {
            throw new InvalidOperationException("Effective date must be set before requesting approval.");
        }

        if (string.IsNullOrWhiteSpace(BodyHtml))
        {
            throw new InvalidOperationException("Template body must not be empty before requesting approval.");
        }

        Status = LeaseTemplateVersionStatus.PendingApproval;
    }

    public void Approve(Guid userId)
    {
        if (Status != LeaseTemplateVersionStatus.PendingApproval)
        {
            throw new InvalidOperationException($"Cannot approve a version in status '{Status}'.");
        }

        if (ApprovedBy is null)
        {
            ApprovedBy = userId;
            ApprovedAt = DateTime.UtcNow;
            return;
        }

        if (ApprovedBy == userId)
        {
            throw new InvalidOperationException("The same user cannot provide both approvals (dual-control).");
        }

        SecondApproverId = userId;
        Status = LeaseTemplateVersionStatus.Active;
    }

    public bool HasDualApproval => ApprovedBy.HasValue && SecondApproverId.HasValue;

    public void Deprecate()
    {
        if (Status != LeaseTemplateVersionStatus.Active)
        {
            throw new InvalidOperationException($"Only active versions can be deprecated. Current status: '{Status}'.");
        }

        Status = LeaseTemplateVersionStatus.Deprecated;
    }

    private void EnsureDraft()
    {
        if (Status != LeaseTemplateVersionStatus.Draft)
        {
            throw new InvalidOperationException($"Version must be in Draft status. Current status: '{Status}'.");
        }
    }
}
