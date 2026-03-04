using Lagedra.SharedKernel.Results;

namespace Lagedra.Auth.Application.Errors;

public static class AuthErrors
{
    public static readonly Error EmailAlreadyExists = new("Auth.EmailAlreadyExists", "A user with this email already exists.");
    public static readonly Error InvalidCredentials = new("Auth.InvalidCredentials", "Email or password is incorrect.");
    public static readonly Error EmailNotVerified = new("Auth.EmailNotVerified", "Please verify your email before logging in.");
    public static readonly Error AccountInactive = new("Auth.AccountInactive", "This account has been deactivated.");
    public static readonly Error InvalidToken = new("Auth.InvalidToken", "The token is invalid or has expired.");
    public static readonly Error TokenAlreadyRevoked = new("Auth.TokenAlreadyRevoked", "The token has already been revoked.");
    public static readonly Error UserNotFound = new("Auth.UserNotFound", "User not found.");
    public static readonly Error PasswordMismatch = new("Auth.PasswordMismatch", "Current password is incorrect.");
    public static readonly Error SelfRoleElevation = new("Auth.SelfRoleElevation", "You cannot change your own role.");

    public static Error IdentityError(string description) =>
        new("Auth.IdentityError", description);
}
