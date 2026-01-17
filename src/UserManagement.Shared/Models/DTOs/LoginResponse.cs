namespace UserManagement.Shared.Models.DTOs;

/// <summary>
/// Response model for user login endpoint.
/// Contains authentication tokens and user information.
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT access token for authenticating subsequent requests.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token for obtaining a new access token when it expires.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Token type, typically "Bearer".
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Number of seconds until the access token expires.
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// User information associated with the login.
    /// </summary>
    public UserDto User { get; set; } = new();
}
