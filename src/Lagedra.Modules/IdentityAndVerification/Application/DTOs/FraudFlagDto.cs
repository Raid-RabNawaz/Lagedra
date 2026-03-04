namespace Lagedra.Modules.IdentityAndVerification.Application.DTOs;

public sealed record FraudFlagDto(
    Guid FlagId,
    Guid UserId,
    string Reason,
    string Source,
    DateTime RaisedAt,
    DateTime SlaDeadline,
    DateTime? ResolvedAt,
    bool IsEscalated);
