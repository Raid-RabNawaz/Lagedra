using Lagedra.Modules.Arbitration.Domain.Enums;

namespace Lagedra.Modules.Arbitration.Domain.Policies;

public static class EvidenceMinimumThresholdPolicy
{
    private static readonly Dictionary<ArbitrationCategory, int> Thresholds = new()
    {
        [ArbitrationCategory.CategoryA] = 3,
        [ArbitrationCategory.CategoryB] = 2,
        [ArbitrationCategory.CategoryC] = 2,
        [ArbitrationCategory.CategoryD] = 4,
        [ArbitrationCategory.CategoryE] = 3,
        [ArbitrationCategory.CategoryF] = 2,
        [ArbitrationCategory.CategoryG] = 1,
        [ArbitrationCategory.Other] = 1
    };

    public static int GetMinimumSlots(ArbitrationCategory category) =>
        Thresholds.TryGetValue(category, out var min) ? min : 1;

    public static bool IsSatisfied(ArbitrationCategory category, int evidenceCount) =>
        evidenceCount >= GetMinimumSlots(category);
}
