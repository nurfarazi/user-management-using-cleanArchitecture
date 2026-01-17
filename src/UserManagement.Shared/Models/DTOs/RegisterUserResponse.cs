namespace UserManagement.Shared.Models.DTOs;

/// <summary>
/// DTO for successful user registration responses.
/// Returns basic user information after successful registration.
/// Password hash is never exposed in responses.
/// </summary>
public class RegisterUserResponse
{
    /// <summary>
    /// MongoDB ObjectId converted to string as unique user identifier.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The registered email address.
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
    /// User's registered phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// UTC timestamp when the user was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
