namespace UserManagement.Shared.Configuration;

/// <summary>
/// JWT configuration settings for token generation and validation.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Secret key for signing JWT tokens.
    /// Should be at least 32 characters long for HS256.
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer claim.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token audience claim.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiration time in minutes.
    /// Defaults to 15 minutes for short-lived tokens.
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Refresh token expiration time in days.
    /// Defaults to 7 days.
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
