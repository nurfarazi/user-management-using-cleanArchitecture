namespace UserManagement.Shared.Models.Entities;

/// <summary>
/// User role enumeration for role-based authorization.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Regular user with basic permissions.
    /// </summary>
    User = 0,

    /// <summary>
    /// Administrator with elevated permissions.
    /// </summary>
    Admin = 1
}

/// <summary>
/// Extension methods for UserRole enumeration.
/// </summary>
public static class UserRoleExtensions
{
    /// <summary>
    /// Converts UserRole enum to string representation.
    /// </summary>
    public static string ToRoleString(this UserRole role)
    {
        return role switch
        {
            UserRole.Admin => "Admin",
            UserRole.User => "User",
            _ => "User"
        };
    }

    /// <summary>
    /// Parses a string to UserRole enum.
    /// </summary>
    public static UserRole FromRoleString(string roleString)
    {
        return roleString?.ToLowerInvariant() switch
        {
            "admin" => UserRole.Admin,
            "user" => UserRole.User,
            _ => UserRole.User
        };
    }
}
