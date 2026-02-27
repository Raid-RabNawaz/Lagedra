using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Lagedra.Auth.Application.Services;

public sealed class JwtTokenService(
    IConfiguration configuration,
    IClock clock,
    IPartnerMembershipProvider? partnerMembershipProvider = null)
{
    private readonly string _secret = configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
    private readonly string _issuer = configuration["Jwt:Issuer"]
        ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
    private readonly string _audience = configuration["Jwt:Audience"]
        ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
    private readonly int _expiryMinutes = int.TryParse(configuration["Jwt:ExpiryMinutes"], out var m) ? m : 15;

    public int ExpirySeconds => _expiryMinutes * 60;

    public async Task<string> GenerateAccessTokenAsync(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("role", user.Role.ToString())
        };

        if (partnerMembershipProvider is not null)
        {
            var orgId = await partnerMembershipProvider
                .GetPartnerOrganizationIdAsync(user.Id)
                .ConfigureAwait(false);

            if (orgId.HasValue)
            {
                claims.Add(new Claim("partner_org_id", orgId.Value.ToString()));
            }
        }

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
