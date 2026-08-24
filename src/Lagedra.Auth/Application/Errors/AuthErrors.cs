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
    public static readonly Error PasswordRequired = new("Auth.PasswordRequired", "A password is required to create an account.");
    public static readonly Error PreLaunchRestricted = new("Auth.PreLaunchRestricted", "Lagedra is launching soon. Hosts can sign in to add listings; partner access opens at launch and we'll email you then.");
    public static readonly Error PhoneRequired = new("Auth.PhoneRequired", "Add a phone number to your profile before verifying.");
    public static readonly Error PhoneInvalid = new("Auth.PhoneInvalid", "Enter a valid phone number, e.g. (555) 123-4567 or +15551234567.");
    public static readonly Error Underage = new("Auth.Underage", "You must be at least 18 years old to use Lagedra.");
    public static readonly Error PhoneAlreadyVerified = new("Auth.PhoneAlreadyVerified", "This phone number is already verified.");
    public static readonly Error InvalidPhoneCode = new("Auth.InvalidPhoneCode", "The verification code is invalid or has expired.");
    public static readonly Error PhoneCodeRateLimited = new("Auth.PhoneCodeRateLimited", "Please wait before requesting another verification code.");

    public static Error IdentityError(string description) =>
        new("Auth.IdentityError", description);
}
