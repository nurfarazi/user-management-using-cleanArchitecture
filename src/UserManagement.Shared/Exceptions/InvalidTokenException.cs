namespace UserManagement.Shared.Exceptions;

/// <summary>
/// Exception thrown when a JWT token is invalid, expired, or revoked.
/// Represents HTTP 401 Unauthorized response.
/// </summary>
public class InvalidTokenException : Exception
{
    /// <summary>
    /// Initializes a new instance of the InvalidTokenException class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public InvalidTokenException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the InvalidTokenException class with inner exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public InvalidTokenException(string message, Exception innerException)
        : base(message, innerException) { }
}
