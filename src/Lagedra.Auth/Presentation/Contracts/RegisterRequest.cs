using Lagedra.Auth.Domain;

namespace Lagedra.Auth.Presentation.Contracts;

public sealed record RegisterRequest(string Email, string Password, UserRole Role);
