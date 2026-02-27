using System.IdentityModel.Tokens.Jwt;
using Google.Apis.Auth;
using Lagedra.Auth.Application.Settings;
using Lagedra.Auth.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Lagedra.Auth.Application.Services;

public sealed record ExternalUserInfo(
    string ProviderKey,
    string Email,
    string? FirstName,
    string? LastName);

public sealed partial class ExternalAuthValidator
{
    private readonly ExternalAuthSettings _settings;
    private readonly ILogger<ExternalAuthValidator> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public ExternalAuthValidator(
        IOptions<ExternalAuthSettings> settings,
        ILogger<ExternalAuthValidator> logger,
        IHttpClientFactory httpClientFactory)
    {
        _settings = settings.Value;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Provider} token validation succeeded but email was empty")]
    private partial void LogEmptyEmail(string provider);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Provider} token validation failed")]
    private partial void LogTokenValidationFailed(Exception ex, string provider);

    public async Task<ExternalUserInfo?> ValidateAsync(
        ExternalAuthProvider provider,
        string idToken,
        CancellationToken ct = default)
    {
        return provider switch
        {
            ExternalAuthProvider.Google => await ValidateGoogleTokenAsync(idToken, ct).ConfigureAwait(false),
            ExternalAuthProvider.Apple => await ValidateAppleTokenAsync(idToken, ct).ConfigureAwait(false),
            ExternalAuthProvider.Microsoft => await ValidateMicrosoftTokenAsync(idToken, ct).ConfigureAwait(false),
            _ => null
        };
    }

    private async Task<ExternalUserInfo?> ValidateGoogleTokenAsync(string idToken, CancellationToken ct)
    {
        try
        {
            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_settings.Google.ClientId]
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                LogEmptyEmail("Google");
                return null;
            }

            return new ExternalUserInfo(
                ProviderKey: payload.Subject,
                Email: payload.Email,
                FirstName: payload.GivenName,
                LastName: payload.FamilyName);
        }
        catch (InvalidJwtException ex)
        {
            LogTokenValidationFailed(ex, "Google");
            return null;
        }
    }

    private async Task<ExternalUserInfo?> ValidateAppleTokenAsync(string idToken, CancellationToken ct)
    {
        try
        {
            const string appleIssuer = "https://appleid.apple.com";
            const string discoveryUrl = "https://appleid.apple.com/.well-known/openid-configuration";

            using var httpClient = _httpClientFactory.CreateClient();
            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                discoveryUrl,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever(httpClient));

            var config = await configManager.GetConfigurationAsync(ct).ConfigureAwait(false);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = appleIssuer,
                ValidateAudience = true,
                ValidAudience = _settings.Apple.ClientId,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = config.SigningKeys,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = await handler.ValidateTokenAsync(idToken, validationParameters).ConfigureAwait(false);

            if (!principal.IsValid)
            {
                // Handle validation failure - you can access validationResult.Exception for details
                throw new SecurityTokenException($"Token validation failed: {principal.Exception?.Message}");
            }

            var email = principal.ClaimsIdentity.FindFirst("email")?.Value
                        ?? principal.ClaimsIdentity.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrWhiteSpace(email))
            {
                LogEmptyEmail("Apple");
                return null;
            }

            var sub = principal.ClaimsIdentity.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                      ?? principal.ClaimsIdentity.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            return new ExternalUserInfo(
                ProviderKey: sub ?? email,
                Email: email,
                FirstName: null,
                LastName: null);
        }
        catch (Exception ex) when (ex is SecurityTokenException or SecurityTokenValidationException)
        {
            LogTokenValidationFailed(ex, "Apple");
            return null;
        }
    }

    private async Task<ExternalUserInfo?> ValidateMicrosoftTokenAsync(string idToken, CancellationToken ct)
    {
        try
        {
            var tenant = _settings.Microsoft.TenantId;
            var discoveryUrl = $"https://login.microsoftonline.com/{tenant}/v2.0/.well-known/openid-configuration";

            using var httpClient = _httpClientFactory.CreateClient();
            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                discoveryUrl,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever(httpClient));

            var config = await configManager.GetConfigurationAsync(ct).ConfigureAwait(false);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = [
                    $"https://login.microsoftonline.com/{tenant}/v2.0",
                    "https://login.microsoftonline.com/9188040d-6c67-4c5b-b112-36a304b66dad/v2.0"
                ],
                ValidateAudience = true,
                ValidAudience = _settings.Microsoft.ClientId,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = config.SigningKeys,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = await handler.ValidateTokenAsync(idToken, validationParameters).ConfigureAwait(true);

            var email = principal.ClaimsIdentity.FindFirst("preferred_username")?.Value
                        ?? principal.ClaimsIdentity.FindFirst("email")?.Value
                        ?? principal.ClaimsIdentity.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrWhiteSpace(email))
            {
                LogEmptyEmail("Microsoft");
                return null;
            }

            var sub = principal.ClaimsIdentity.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                      ?? principal.ClaimsIdentity.FindFirst("oid")?.Value
                      ?? principal.ClaimsIdentity.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var firstName = principal.ClaimsIdentity.FindFirst("given_name")?.Value
                            ?? principal.ClaimsIdentity.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value;
            var lastName = principal.ClaimsIdentity.FindFirst("family_name")?.Value
                           ?? principal.ClaimsIdentity.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value;

            return new ExternalUserInfo(
                ProviderKey: sub ?? email,
                Email: email,
                FirstName: firstName,
                LastName: lastName);
        }
        catch (Exception ex) when (ex is SecurityTokenException or SecurityTokenValidationException)
        {
            LogTokenValidationFailed(ex, "Microsoft");
            return null;
        }
    }
}
