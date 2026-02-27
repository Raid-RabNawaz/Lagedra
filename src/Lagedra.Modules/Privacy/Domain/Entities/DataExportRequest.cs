using Lagedra.Modules.Privacy.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Privacy.Domain.Entities;

public sealed class DataExportRequest : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public ExportStatus Status { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Uri? PackageUrl { get; private set; }

    private DataExportRequest() { }

    public static DataExportRequest Create(Guid userId)
    {
        return new DataExportRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = ExportStatus.Queued,
            RequestedAt = DateTime.UtcNow
        };
    }

    public void MarkProcessing()
    {
        if (Status != ExportStatus.Queued)
        {
            throw new InvalidOperationException($"Cannot start processing export in status '{Status}'.");
        }

        Status = ExportStatus.Processing;
    }

    public void Complete(Uri packageUrl)
    {
        ArgumentNullException.ThrowIfNull(packageUrl);

        Status = ExportStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        PackageUrl = packageUrl;
    }

    public void Fail()
    {
        Status = ExportStatus.Failed;
        CompletedAt = DateTime.UtcNow;
    }
}
