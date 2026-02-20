using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Lagedra.Auth.Application.Services;

public sealed class JwtTokenService(IConfiguration configuration, IClock clock)
{
    private readonly string _secret = configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
    private readonly string _issuer = configuration["Jwt:Issuer"]
        ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
    private readonly string _audience = configuration["Jwt:Audience"]
        ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
    private readonly int _expiryMinutes = int.TryParse(configuration["Jwt:ExpiryMinutes"], out var m) ? m : 15;

    public int ExpirySeconds => _expiryMinutes * 60;

    public string GenerateAccessToken(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("role", user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: clock.UtcNow,
            expires: clock.UtcNow.AddMinutes(_expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Validates an access token's signature and claims without checking expiry.
    /// Used exclusively during token refresh: the caller already holds a valid
    /// <see cref="Domain.RefreshToken"/>, so lifetime is intentionally not re-checked here.
    /// </summary>
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secret);

#pragma warning disable CA5404 // ValidateLifetime is intentionally false: refresh-token flow validates expiry via the RefreshToken entity, not the access token lifetime
        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = false,
            ClockSkew = TimeSpan.Zero
        };
#pragma warning restore CA5404

        try
        {
            return tokenHandler.ValidateToken(token, parameters, out _);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }
}
