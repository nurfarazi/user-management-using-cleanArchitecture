namespace UserManagement.Shared.Models.DTOs;

/// <summary>
/// DTO for password change requests.
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// The user's current password for verification.
    /// </summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// The new password to set.
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation of the new password.
    /// </summary>
    public string ConfirmPassword { get; set; } = string.Empty;
}
