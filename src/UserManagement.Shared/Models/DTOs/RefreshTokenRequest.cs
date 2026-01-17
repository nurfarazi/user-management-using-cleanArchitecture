namespace UserManagement.Shared.Models.DTOs;

/// <summary>
/// Request model for refreshing an access token.
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// The refresh token obtained during login.
    /// Used to obtain a new access token.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}
