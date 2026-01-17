namespace UserManagement.Shared.Models.DTOs;

/// <summary>
/// Data transfer object for user information.
/// Contains public user data without sensitive information (password hash).
/// </summary>
public class UserDto
{
    /// <summary>
    /// Unique user identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User's display name. Optional.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// User's date of birth. Optional.
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// User's phone number. Optional.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// User's role (User, Admin, etc.).
    /// </summary>
    public string Role { get; set; } = "User";

    /// <summary>
    /// User account status.
    /// </summary>
    public string Status { get; set; } = "PendingVerification";

    /// <summary>
    /// User account creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User account last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Optimistic concurrency version (ETag).
    /// </summary>
    public int Version { get; set; }
}
