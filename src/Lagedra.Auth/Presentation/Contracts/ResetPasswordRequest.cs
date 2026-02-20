namespace Lagedra.Auth.Presentation.Contracts;

public sealed record ResetPasswordRequest(Guid UserId, string Token, string NewPassword);
