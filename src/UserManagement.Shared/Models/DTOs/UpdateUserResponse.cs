namespace UserManagement.Shared.Models.DTOs;

/// <summary>
/// DTO for successful user update responses.
/// </summary>
public class UpdateUserResponse
{
    /// <summary>
    /// Unique user identifier.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The email address (cannot be updated).
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
    /// User's display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// User's date of birth.
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// User's phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// UTC timestamp when the user was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// The new version/ETag of the profile.
    /// </summary>
    public int Version { get; set; }
}
