using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NationalWid.Api.Auth;
using NationalWid.Api.Models;
using NationalWid.Api.Services;

namespace NationalWid.Api.Controllers;

/// <summary>
/// Manages user-owned API keys that allow external systems to authenticate against the
/// National WID API without interactive Cognito flows.
///
/// Authentication: Requires either a valid Cognito JWT or an active API key on every endpoint.
/// Ownership: Each user can only see and manage their own keys — the Cognito <c>sub</c> (username)
/// from the token is used implicitly; callers cannot supply or override it.
///
/// Key lifecycle:
///   <list type="bullet">
///     <item>POST   — create a new key (plaintext returned once, never again)</item>
///     <item>GET    — list all keys for the authenticated user</item>
///     <item>PATCH  — update description, status, or expiry</item>
///     <item>DELETE — permanently revoke a key</item>
///   </list>
/// </summary>
[ApiController]
[Route("api-keys")]
[Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},{ApiKeyAuthHandler.SchemeName}")]
public sealed class ApiKeysController(ApiKeyService keyService) : ControllerBase
{
    private string Username =>
        User.FindFirst("sub")?.Value
        ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("Could not resolve username from token.");

    // -------------------------------------------------------------------------
    // POST /api-keys  —  create a new key
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a new API key for the authenticated user.
    /// The <c>plaintextKey</c> field in the response is returned <b>exactly once</b> and
    /// is never stored or recoverable.  Store it in a secrets manager immediately.
    /// </summary>
    /// <param name="request">Key creation options (description, expiry, rate profile).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HTTP 201 with the new key metadata including the one-time plaintext value.</returns>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await keyService.CreateAsync(
            Username,
            request.Description,
            request.ExpiresAt,
            request.RateProfile ?? "standard",
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { keyId = result.KeyId }, result);
    }

    // -------------------------------------------------------------------------
    // GET /api-keys  —  list all keys for the authenticated user
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns a list of all API keys for the authenticated user.
    /// Key hashes and plaintext values are never included in the response.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HTTP 200 with an array of <see cref="ApiKeyInfo"/> records.</returns>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var keys = await keyService.ListForUserAsync(Username, cancellationToken);
        return Ok(keys);
    }

    // -------------------------------------------------------------------------
    // GET /api-keys/{keyId}  —  get a single key
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns metadata for a single API key owned by the authenticated user.
    /// </summary>
    /// <param name="keyId">Key UUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HTTP 200 with <see cref="ApiKeyInfo"/>, or HTTP 404 if not found.</returns>
    [HttpGet("{keyId:guid}")]
    public async Task<IActionResult> Get(Guid keyId, CancellationToken cancellationToken)
    {
        var info = await keyService.GetForUserAsync(Username, keyId, cancellationToken);
        return info is null ? NotFound() : Ok(info);
    }

    // -------------------------------------------------------------------------
    // PATCH /api-keys/{keyId}  —  update description, status, or expiry
    // -------------------------------------------------------------------------

    /// <summary>
    /// Partially updates an API key owned by the authenticated user.
    /// Omitted fields are left unchanged.
    /// <list type="bullet">
    ///   <item><c>status</c> — toggle between <c>active</c> and <c>disabled</c></item>
    ///   <item><c>expiresAt</c> — set a new expiry; pass <c>null</c> to remove the expiry</item>
    ///   <item><c>description</c> — update the label</item>
    /// </list>
    /// To permanently revoke, use DELETE instead.
    /// </summary>
    /// <param name="keyId">Key UUID.</param>
    /// <param name="request">Patch payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HTTP 200 with updated <see cref="ApiKeyInfo"/>, or HTTP 404 if not found.</returns>
    [HttpPatch("{keyId:guid}")]
    public async Task<IActionResult> Patch(
        Guid keyId,
        [FromBody] PatchApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        var info = await keyService.PatchAsync(Username, keyId, request, cancellationToken);
        return info is null ? NotFound() : Ok(info);
    }

    // -------------------------------------------------------------------------
    // DELETE /api-keys/{keyId}  —  permanently revoke a key
    // -------------------------------------------------------------------------

    /// <summary>
    /// Permanently revokes an API key.
    /// Revoked keys cannot be reactivated; create a new key if access is still needed.
    /// </summary>
    /// <param name="keyId">Key UUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HTTP 204 on success, or HTTP 404 if the key was not found.</returns>
    [HttpDelete("{keyId:guid}")]
    public async Task<IActionResult> Revoke(Guid keyId, CancellationToken cancellationToken)
    {
        var ok = await keyService.RevokeAsync(Username, keyId, cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}

/// <summary>Request body for <c>POST /api-keys</c>.</summary>
public sealed record CreateApiKeyRequest
{
    /// <summary>Optional human-readable label to help identify the key later.</summary>
    public string? Description { get; init; }

    /// <summary>UTC expiry for the key; <c>null</c> means the key never expires.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Named rate-limiting profile; defaults to <c>standard</c> if omitted.</summary>
    public string? RateProfile { get; init; }
}
