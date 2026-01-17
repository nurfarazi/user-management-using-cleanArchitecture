namespace UserManagement.Shared.Models.DTOs;

/// <summary>
/// Request model for user login endpoint.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User's email address for authentication.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's password for authentication.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
