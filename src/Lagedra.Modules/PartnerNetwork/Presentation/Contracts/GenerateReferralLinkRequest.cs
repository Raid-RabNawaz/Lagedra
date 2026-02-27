namespace Lagedra.Modules.PartnerNetwork.Presentation.Contracts;

public sealed record GenerateReferralLinkRequest(DateTime? ExpiresAt, int? MaxUses);
