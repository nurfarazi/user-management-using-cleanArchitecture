namespace UserManagement.Shared.Models.Entities;

/// <summary>
/// User account status enumeration.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// Account created but email/phone verification pending.
    /// </summary>
    PendingVerification = 0,

    /// <summary>
    /// Account is active and fully functional.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Account is deactivated by admin or user.
    /// </summary>
    Deactivated = 2,

    /// <summary>
    /// Account is permanently banned for policy violations.
    /// </summary>
    Banned = 3
}

/// <summary>
/// Extension methods for UserStatus enumeration.
/// </summary>
public static class UserStatusExtensions
{
    public static string ToStatusString(this UserStatus status)
    {
        return status switch
        {
            UserStatus.PendingVerification => "PendingVerification",
            UserStatus.Active => "Active",
            UserStatus.Deactivated => "Deactivated",
            UserStatus.Banned => "Banned",
            _ => "PendingVerification"
        };
    }

    public static UserStatus FromStatusString(string statusString)
    {
        return statusString?.ToLowerInvariant() switch
        {
            "pendingverification" => UserStatus.PendingVerification,
            "active" => UserStatus.Active,
            "deactivated" => UserStatus.Deactivated,
            "banned" => UserStatus.Banned,
            _ => UserStatus.PendingVerification
        };
    }
}
