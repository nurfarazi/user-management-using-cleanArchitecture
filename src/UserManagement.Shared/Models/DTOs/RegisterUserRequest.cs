namespace UserManagement.Shared.Models.DTOs;

/// <summary>
/// DTO for user registration requests from the API.
/// Contains all required data for creating a new user account.
/// </summary>
public class RegisterUserRequest
{
    /// <summary>
    /// User's email address. Required and must be unique.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's password in plain text. Will be hashed before storage.
    /// Must meet complexity requirements (min 8 chars, upper, lower, digit, special char).
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// User's first name. Required.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name. Required.
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
    /// Version of terms and conditions accepted by user. Required.
    /// </summary>
    public string TermsAcceptedVersion { get; set; } = string.Empty;

    /// <summary>
    /// Version of privacy policy accepted by user. Required.
    /// </summary>
    public string PrivacyPolicyAcceptedVersion { get; set; } = string.Empty;

    /// <summary>
    /// Client IP address (set by server).
    /// </summary>
    public string? RegistrationIp { get; set; }

    /// <summary>
    /// Client User Agent string (set by server).
    /// </summary>
    public string? RegistrationUserAgent { get; set; }
}
