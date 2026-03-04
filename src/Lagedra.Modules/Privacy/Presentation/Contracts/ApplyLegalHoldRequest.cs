namespace Lagedra.Modules.Privacy.Presentation.Contracts;

public sealed record ApplyLegalHoldRequest(Guid UserId, string Reason);
