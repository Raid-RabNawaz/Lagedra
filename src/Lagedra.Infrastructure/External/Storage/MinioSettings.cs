namespace Lagedra.Infrastructure.External.Storage;

public sealed class MinioSettings
{
    public const string SectionName = "MinIO";

    public required string Endpoint { get; init; }
    public required string AccessKey { get; init; }
    public required string SecretKey { get; init; }
    public string EvidenceBucket { get; init; } = "lagedra-evidence";
    public string ExportsBucket { get; init; } = "lagedra-exports";
    public bool UseHttps { get; init; }
}
