namespace UserManagement.Shared.Exceptions;

/// <summary>
/// Exception thrown when a user attempts an action they don't have permission for.
/// Represents HTTP 403 Forbidden response.
/// </summary>
public class ForbiddenException : Exception
{
    /// <summary>
    /// Initializes a new instance of the ForbiddenException class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ForbiddenException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the ForbiddenException class with inner exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ForbiddenException(string message, Exception innerException)
        : base(message, innerException) { }
}
