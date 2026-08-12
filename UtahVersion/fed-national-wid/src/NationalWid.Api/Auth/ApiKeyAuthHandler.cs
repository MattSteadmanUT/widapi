using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using NationalWid.Api.Models;
using NationalWid.Api.Services;

namespace NationalWid.Api.Auth;

/// <summary>
/// ASP.NET Core authentication handler for the <c>ApiKey</c> scheme.
///
/// Recognises requests that carry an <c>Authorization: ApiKey &lt;key&gt;</c> header and
/// validates the key against the database via <see cref="ApiKeyService"/>.
///
/// On success it builds a <see cref="ClaimsPrincipal"/> that includes:
///   • <c>sub</c> — the Cognito username stored with the key
///   • <c>custom:stFips</c> — intentionally omitted; API key callers get global (unrestricted) access
///     within the scopes allowed by Cognito policies for that username
///   • <c>auth_method</c> = <c>apikey</c> — lets downstream code distinguish key vs JWT callers
///
/// If no <c>Authorization: ApiKey …</c> header is present the handler returns
/// <see cref="AuthenticateResult.NoResult"/> so that the JWT bearer handler can take over.
/// </summary>
public sealed class ApiKeyAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ApiKeyService keyService)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiKey";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return AuthenticateResult.NoResult();

        var headerValue = authHeader.ToString();
        if (!headerValue.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var plaintextKey = headerValue["ApiKey ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(plaintextKey))
            return AuthenticateResult.Fail("Empty API key.");

        ApiKey? apiKey;
        try
        {
            apiKey = await keyService.ValidateAsync(plaintextKey, Context.RequestAborted);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating API key.");
            return AuthenticateResult.Fail("API key validation error.");
        }

        if (apiKey is null)
            return AuthenticateResult.Fail("Invalid or expired API key.");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, apiKey.Username),
            new Claim("sub",         apiKey.Username),
            new Claim("auth_method", "apikey"),
            new Claim("apikey_id",   apiKey.KeyId.ToString()),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }
}
