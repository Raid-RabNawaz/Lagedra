namespace Lagedra.Modules.Arbitration.Domain.Policies;

public static class ArbitratorCaseloadPolicy
{
    public const int SoftCap = 15;
    public const int HardCap = 20;

    public static bool IsAtHardCap(int activeCaseCount) => activeCaseCount >= HardCap;

    public static bool IsOverSoftCap(int activeCaseCount) => activeCaseCount >= SoftCap;
}
