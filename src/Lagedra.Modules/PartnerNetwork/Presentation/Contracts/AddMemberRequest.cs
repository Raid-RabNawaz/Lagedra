using Lagedra.Modules.PartnerNetwork.Domain.Enums;

namespace Lagedra.Modules.PartnerNetwork.Presentation.Contracts;

public sealed record AddMemberRequest(Guid UserId, PartnerMemberRole Role);
