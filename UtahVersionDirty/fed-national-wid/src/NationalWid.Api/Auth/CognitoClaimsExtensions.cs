using System.Security.Claims;

namespace NationalWid.Api.Auth;

/// <summary>
/// Helpers for reading ULMITA custom claims off the Cognito-issued JWT.
/// </summary>
public static class CognitoClaimsExtensions
{
    public const string StFipsClaim = "custom:stFips";
    public const string ActivatedClaim = "custom:ulmita_activated";
    public const string RolesClaim = "custom:ulmita_roles";

    /// <summary>
    /// The 2-digit state FIPS code identifying which state the caller represents
    /// (e.g. "49" for Utah), or null when the claim is absent.
    /// </summary>
    public static string? GetStFips(this ClaimsPrincipal user) =>
        user.FindFirstValue(StFipsClaim);

    /// <summary>True when the ULMITA account has been activated.</summary>
    public static bool IsUlmitaActivated(this ClaimsPrincipal user) =>
        string.Equals(user.FindFirstValue(ActivatedClaim), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>Parses the comma-separated ULMITA role list; empty when the claim is absent.</summary>
    public static IReadOnlyList<string> GetUlmitaRoles(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(RolesClaim);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
