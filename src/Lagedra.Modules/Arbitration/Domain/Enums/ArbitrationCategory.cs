namespace Lagedra.Modules.Arbitration.Domain.Enums;

public enum ArbitrationCategory
{
    CategoryA,
    CategoryB,
    CategoryC,
    CategoryD,
    CategoryE,
    CategoryF,
    CategoryG,

    /// <summary>
    /// Security deposit not returned (or under-returned) by the host after
    /// move-out. Raised by the tenant when the deposit-return handshake fails.
    /// </summary>
    DepositReturn,

    Other
}
