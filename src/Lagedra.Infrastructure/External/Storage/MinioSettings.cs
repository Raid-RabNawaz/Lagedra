using System.Diagnostics.CodeAnalysis;

namespace Lagedra.Infrastructure.External.Storage;

public sealed class MinioSettings
{
    public const string SectionName = "MinIO";

    public required string Endpoint { get; init; }
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    // Physical bucket layout
    // ─────────────────────────────────────────────────────────────────
    // To stay cheap on AWS we group buckets by *access pattern* (the only
    // real security boundary) rather than by feature. Defaults collapse the
    // four logical pools onto two physical buckets:
    //
    //   lagedra-private  ← evidence/, exports/, quarantine/   (no public ACL,
    //                                                          presigned URLs)
    //   lagedra-public   ← listings/, avatars/                (public-read,
    //                                                          CDN-friendly)
    //
    // The four feature-named keys remain so any single feature can be split
    // back out per-environment (e.g. Object-Lock for evidence in prod) by
    // overriding `MinIO:EvidenceBucket` in configuration — no code change
    // required.
    public string EvidenceBucket { get; init; } = "lagedra-private";
    public string ExportsBucket { get; init; } = "lagedra-private";
    public string QuarantineBucket { get; init; } = "lagedra-private";
    public string KycBucket { get; init; } = "lagedra-private";
    public string ListingsBucket { get; init; } = "lagedra-public";
    public string UsersBucket { get; init; } = "lagedra-public";
    public bool UseHttps { get; init; }
    public bool UseIamRole { get; init; }

    /// <summary>
    /// Optional CDN / proxy base URL used when constructing public object URLs
    /// (e.g. "https://cdn.lagedra.com"). When empty, URLs are built from the
    /// configured <see cref="Endpoint"/> using path-style addressing.
    /// </summary>
    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings",
        Justification = "Bound from configuration; allows empty string to disable.")]
    public string PublicBaseUrl { get; init; } = string.Empty;
}
