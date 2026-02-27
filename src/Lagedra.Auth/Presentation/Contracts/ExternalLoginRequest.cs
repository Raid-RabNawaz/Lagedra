using Lagedra.Auth.Domain;

namespace Lagedra.Auth.Presentation.Contracts;

public sealed record ExternalLoginRequest(
    string Provider,
    string IdToken,
    UserRole? PreferredRole = null);
