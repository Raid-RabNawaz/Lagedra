using Lagedra.Modules.Privacy.Domain.Enums;

namespace Lagedra.Modules.Privacy.Presentation.Contracts;

public sealed record ConsentRequest(
    Guid UserId,
    ConsentType ConsentType,
    string IpAddress,
    string UserAgent);
