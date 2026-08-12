namespace NationalWid.Api.Models;

/// <summary>
/// A user-managed API key that allows external systems to authenticate against the National WID API
/// without using interactive Cognito flows.
///
/// Only the SHA-256 digest of the plaintext key is persisted; the plaintext is returned once at
/// creation and is never stored or recoverable after that point.
/// </summary>
public sealed record ApiKey
{
    /// <summary>Stable UUID identifier for this key — safe to share with the owner.</summary>
    public Guid KeyId { get; init; }

    /// <summary>Cognito username of the account that owns this key.</summary>
    public string Username { get; init; } = "";

    /// <summary>
    /// SHA-256 hex digest (lower-case, 64 chars) of the plaintext key.
    /// Used for constant-time equality checking on incoming requests.
    /// </summary>
    public string KeyHash { get; init; } = "";

    /// <summary>
    /// First 8 characters of the original plaintext key.
    /// Shown in listing responses so owners can identify their keys without exposing secrets.
    /// </summary>
    public string KeyPrefix { get; init; } = "";

    /// <summary>Optional human-readable label assigned by the owner at creation time.</summary>
    public string? Description { get; init; }

    /// <summary>
    /// Lifecycle state of the key.
    /// Valid values: <c>active</c>, <c>disabled</c>, <c>revoked</c>, <c>expired</c>.
    /// Only <c>active</c> keys pass authentication.
    /// </summary>
    public string Status { get; init; } = "active";

    /// <summary>UTC timestamp when the key was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>UTC expiry time; <c>null</c> means the key never expires on its own.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>UTC timestamp of the most recent successful use of this key, if any.</summary>
    public DateTimeOffset? LastUsedAt { get; init; }

    /// <summary>UTC timestamp when the key was revoked, if applicable.</summary>
    public DateTimeOffset? RevokedAt { get; init; }

    /// <summary>
    /// Named rate-limiting profile applied to requests authenticated with this key.
    /// Profiles are resolved from the <c>apikeyratepolicies</c> table so limits can be
    /// changed without redeploying code.
    /// </summary>
    public string RateProfile { get; init; } = "standard";
}

/// <summary>
/// Response body returned once at key creation.
/// The <c>PlaintextKey</c> field is never stored and will not appear again after this response.
/// </summary>
public sealed record ApiKeyCreatedResponse
{
    /// <summary>Stable key identifier — use in PATCH / DELETE calls.</summary>
    public Guid KeyId { get; init; }

    /// <summary>
    /// The full plaintext API key.  Store it securely immediately — it cannot be retrieved later.
    /// Pass as the <c>Authorization: ApiKey &lt;key&gt;</c> header on subsequent API requests.
    /// </summary>
    public string PlaintextKey { get; init; } = "";

    /// <summary>Display prefix (first 8 chars) shown in subsequent listing responses.</summary>
    public string KeyPrefix { get; init; } = "";

    /// <summary>User-provided label.</summary>
    public string? Description { get; init; }

    /// <summary>Active status immediately after creation.</summary>
    public string Status { get; init; } = "active";

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>UTC expiry time, or <c>null</c> for no expiry.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Rate-limiting profile name.</summary>
    public string RateProfile { get; init; } = "standard";
}

/// <summary>
/// Safe key metadata returned in listing and GET responses.
/// The plaintext key and hash are never included.
/// </summary>
public sealed record ApiKeyInfo
{
    /// <summary>Stable key identifier.</summary>
    public Guid KeyId { get; init; }

    /// <summary>Display prefix (first 8 chars).</summary>
    public string KeyPrefix { get; init; } = "";

    /// <summary>User-provided label.</summary>
    public string? Description { get; init; }

    /// <summary>Lifecycle state: <c>active</c>, <c>disabled</c>, <c>revoked</c>, or <c>expired</c>.</summary>
    public string Status { get; init; } = "";

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>UTC expiry time, or <c>null</c> for no expiry.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>UTC timestamp of most recent successful use, if any.</summary>
    public DateTimeOffset? LastUsedAt { get; init; }

    /// <summary>UTC revocation timestamp, or <c>null</c> if not revoked.</summary>
    public DateTimeOffset? RevokedAt { get; init; }

    /// <summary>Rate-limiting profile name.</summary>
    public string RateProfile { get; init; } = "";
}

/// <summary>Request body for <c>PATCH /api-keys/{keyId}</c>.</summary>
public sealed record PatchApiKeyRequest
{
    /// <summary>When provided, replaces the current description label.</summary>
    public string? Description { get; init; }

    /// <summary>
    /// When provided, transitions key status.
    /// Allowed transitions: <c>active → disabled</c>, <c>disabled → active</c>.
    /// Use <c>DELETE /api-keys/{keyId}</c> to revoke permanently.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// When provided, sets a new UTC expiry.  Pass <c>null</c> explicitly to remove the expiry.
    /// The JSON field must be present and null-valued to clear it; omitting the field means no change.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Flag indicating whether <c>ExpiresAt</c> was included in the JSON body (even if null).</summary>
    public bool ExpiresAtProvided { get; init; }
}
