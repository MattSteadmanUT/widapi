using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NationalWid.Api.Data;
using NationalWid.Api.Models;

namespace NationalWid.Api.Services;

/// <summary>
/// Handles creation, lookup, and lifecycle management of user-managed API keys.
///
/// Security contract:
///   • The plaintext key (32 random bytes, base64url encoded) is returned once at creation.
///   • Only the SHA-256 hex digest is persisted; the plaintext is never stored.
///   • Lookup uses constant-time hash comparison via a DB index on <c>keyhash</c>.
///   • Keys expire automatically when <c>expiresat</c> is in the past.
/// </summary>
public sealed class ApiKeyService(WIDDbContext db)
{
    // Scrypt or Argon2 would add brute-force resistance but at the cost of latency on every API
    // request. SHA-256 is acceptable here because the 32-byte key has 256 bits of entropy — a
    // brute-force attack against the hash is infeasible even without key-stretching.
    private const int KeyLengthBytes = 32;

    /// <summary>
    /// Generates a new API key for the given Cognito username.
    /// Returns the plaintext key in the response — it will not be available again.
    /// </summary>
    /// <param name="username">Cognito username of the requesting user.</param>
    /// <param name="description">Optional human-readable label for the key.</param>
    /// <param name="expiresAt">Optional UTC expiry; pass <c>null</c> for a non-expiring key.</param>
    /// <param name="rateProfile">Named throttle profile; defaults to <c>standard</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<ApiKeyCreatedResponse> CreateAsync(
        string username,
        string? description,
        DateTimeOffset? expiresAt,
        string rateProfile,
        CancellationToken cancellationToken)
    {
        var plaintextBytes = RandomNumberGenerator.GetBytes(KeyLengthBytes);
        var plaintextKey = Convert.ToBase64String(plaintextBytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('='); // base64url

        var keyHash = ComputeHash(plaintextKey);
        var keyPrefix = plaintextKey[..8];

        var entity = new ApiKey
        {
            KeyId = Guid.NewGuid(),
            Username = username,
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            Description = description,
            Status = "active",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
            RateProfile = rateProfile,
        };

        db.ApiKeys.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return new ApiKeyCreatedResponse
        {
            KeyId = entity.KeyId,
            PlaintextKey = plaintextKey,
            KeyPrefix = keyPrefix,
            Description = description,
            Status = "active",
            CreatedAt = entity.CreatedAt,
            ExpiresAt = expiresAt,
            RateProfile = rateProfile,
        };
    }

    /// <summary>
    /// Attempts to resolve an incoming plaintext API key to a valid, active, non-expired DB record.
    /// Returns <c>null</c> when the key is not found, disabled, revoked, or expired.
    /// Also updates <c>lastusedAt</c> on a successful match.
    /// </summary>
    /// <param name="plaintextKey">The raw key from the Authorization header.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<ApiKey?> ValidateAsync(string plaintextKey, CancellationToken cancellationToken)
    {
        var hash = ComputeHash(plaintextKey);
        var key = await db.ApiKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.KeyHash == hash, cancellationToken);

        if (key is null) return null;
        if (key.Status != "active") return null;
        if (key.ExpiresAt.HasValue && key.ExpiresAt.Value <= DateTimeOffset.UtcNow) return null;

        // Best-effort lastUsedAt update — do not fail the request if this write fails.
        try
        {
            await db.ApiKeys
                .Where(k => k.KeyId == key.KeyId)
                .ExecuteUpdateAsync(s => s.SetProperty(k => k.LastUsedAt, DateTimeOffset.UtcNow), cancellationToken);
        }
        catch
        {
            // Non-critical — swallow errors so auth is not blocked by a failed timestamp update.
        }

        return key;
    }

    /// <summary>Returns all keys owned by the given username, most recently created first.</summary>
    public async Task<List<ApiKeyInfo>> ListForUserAsync(string username, CancellationToken cancellationToken)
    {
        return await db.ApiKeys
            .AsNoTracking()
            .Where(k => k.Username == username)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => ToInfo(k))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Returns a single key belonging to the given username, or <c>null</c> if not found
    /// or the key belongs to a different user.
    /// </summary>
    public async Task<ApiKeyInfo?> GetForUserAsync(string username, Guid keyId, CancellationToken cancellationToken)
    {
        var key = await db.ApiKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.KeyId == keyId && k.Username == username, cancellationToken);

        return key is null ? null : ToInfo(key);
    }

    /// <summary>
    /// Applies a partial update to a key.
    /// Only fields explicitly provided in <paramref name="patch"/> are modified.
    /// Returns the updated key info, or <c>null</c> if the key was not found for this user.
    /// </summary>
    public async Task<ApiKeyInfo?> PatchAsync(
        string username,
        Guid keyId,
        PatchApiKeyRequest patch,
        CancellationToken cancellationToken)
    {
        var key = await db.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyId == keyId && k.Username == username, cancellationToken);

        if (key is null) return null;

        // Validate status transition when provided.
        if (patch.Status is not null)
        {
            if (patch.Status is not ("active" or "disabled"))
                throw new BadHttpRequestException(
                    "status must be 'active' or 'disabled'. To permanently revoke a key use DELETE.");

            if (key.Status == "revoked")
                throw new BadHttpRequestException("A revoked key cannot be reactivated.");
        }

        var updated = key with
        {
            Description  = patch.Description  ?? key.Description,
            Status       = patch.Status       ?? key.Status,
            ExpiresAt    = patch.ExpiresAtProvided ? patch.ExpiresAt : key.ExpiresAt,
        };

        db.ApiKeys.Entry(key).CurrentValues.SetValues(updated);
        await db.SaveChangesAsync(cancellationToken);
        return ToInfo(updated);
    }

    /// <summary>
    /// Permanently revokes a key for the given user.
    /// Returns <c>false</c> if the key was not found or belongs to a different user.
    /// </summary>
    public async Task<bool> RevokeAsync(string username, Guid keyId, CancellationToken cancellationToken)
    {
        var key = await db.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyId == keyId && k.Username == username, cancellationToken);

        if (key is null) return false;

        var revoked = key with { Status = "revoked", RevokedAt = DateTimeOffset.UtcNow };
        db.ApiKeys.Entry(key).CurrentValues.SetValues(revoked);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static string ComputeHash(string plaintextKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(plaintextKey);
        var hashBytes = SHA256.HashData(keyBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static ApiKeyInfo ToInfo(ApiKey k) => new()
    {
        KeyId       = k.KeyId,
        KeyPrefix   = k.KeyPrefix,
        Description = k.Description,
        Status      = k.Status,
        CreatedAt   = k.CreatedAt,
        ExpiresAt   = k.ExpiresAt,
        LastUsedAt  = k.LastUsedAt,
        RevokedAt   = k.RevokedAt,
        RateProfile = k.RateProfile,
    };
}
