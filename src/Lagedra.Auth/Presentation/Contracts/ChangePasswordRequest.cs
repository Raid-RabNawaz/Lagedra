namespace Lagedra.Auth.Presentation.Contracts;

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);
