using Lagedra.Modules.Arbitration.Domain.Enums;

namespace Lagedra.Modules.Arbitration.Domain.Policies;

public sealed class ArbitrationFeeSettings
{
    public const string SectionName = "ArbitrationFee";

    public long ProtocolAdjudicationFeeCents { get; set; } = 4900;

    public long BindingArbitrationFeeCents { get; set; } = 9900;

    public long GetFilingFee(ArbitrationTier tier) => tier switch
    {
        ArbitrationTier.ProtocolAdjudication => ProtocolAdjudicationFeeCents,
        ArbitrationTier.BindingArbitration => BindingArbitrationFeeCents,
        _ => ProtocolAdjudicationFeeCents
    };
}
