namespace Lagedra.Modules.InsuranceIntegration.Domain.Policies;

public static class UnknownGraceWindowPolicy
{
    public const int GraceHours = 72;

    public static bool IsBreached(DateTime unknownSince, DateTime utcNow) =>
        (utcNow - unknownSince).TotalHours >= GraceHours;

    public static bool IsTenantInaction(bool hasManualUploadAttempt) =>
        !hasManualUploadAttempt;
}
