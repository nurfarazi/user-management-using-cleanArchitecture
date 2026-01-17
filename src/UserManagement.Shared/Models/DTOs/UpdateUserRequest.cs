namespace UserManagement.Shared.Models.DTOs;

/// <summary>
/// DTO for updating an existing user's profile information.
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// The unique identifier of the user to update.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// User's first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Optional display name for the user profile.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// User's date of birth. Must be 13+ years old if provided.
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Optional phone number for contact purposes. Must follow E.164 and be unique.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Optimistic concurrency version (ETag).
    /// </summary>
    public int Version { get; set; }
}
