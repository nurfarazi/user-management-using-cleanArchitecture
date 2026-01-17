namespace UserManagement.Shared.Models.DTOs;

/// <summary>
/// DTO for updating a user's account status.
/// </summary>
public class UpdateUserStatusRequest
{
    /// <summary>
    /// The new status for the account (e.g., Active, Deactivated, Banned).
    /// </summary>
    public string NewStatus { get; set; } = string.Empty;

    /// <summary>
    /// Reason code for the status change (required for deactivation/banning).
    /// </summary>
    public string ReasonCode { get; set; } = string.Empty;

    /// <summary>
    /// Optional additional details or evidence for the status change.
    /// </summary>
    public string? ReasonDetails { get; set; }
}
