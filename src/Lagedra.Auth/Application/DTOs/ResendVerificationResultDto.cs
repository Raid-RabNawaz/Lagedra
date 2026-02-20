namespace Lagedra.Auth.Application.DTOs;

public sealed record ResendVerificationResultDto(
    bool Sent,
    Uri? VerificationUrl,
    string? VerificationToken)
{
    /// <summary>
    /// Returns a blind success (user not found or already verified).
    /// Indistinguishable from a real send to prevent email enumeration.
    /// </summary>
    public static ResendVerificationResultDto Blind() =>
        new(Sent: false, VerificationUrl: null, VerificationToken: null);
}
