namespace UserManagement.Shared.Exceptions;

/// <summary>
/// Exception thrown when attempting to register a user with an email that already exists.
/// This is a domain-specific exception representing a business rule violation.
/// </summary>
public class UserAlreadyExistsException : Exception
{
    /// <summary>
    /// Initializes a new instance of the UserAlreadyExistsException class.
    /// </summary>
    /// <param name="email">The email address that already exists in the system.</param>
    public UserAlreadyExistsException(string email)
        : base($"A user with email '{email}' already exists in the system.")
    {
        Email = email;
    }

    /// <summary>
    /// Gets the email address that caused the conflict.
    /// </summary>
    public string Email { get; }
}
