namespace UserManagement.Shared.Models.Results;

/// <summary>
/// Generic wrapper for all API responses.
/// Ensures consistent response structure across all endpoints.
/// This follows the REST API best practices for structured responses.
/// </summary>
/// <typeparam name="T">The type of the data payload.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the API call was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The response data payload. Null if the operation failed.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Human-readable message describing the result.
    /// Should be non-technical and suitable for client display.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// List of errors that occurred during the operation.
    /// Empty if the operation was successful.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// UTC timestamp when the response was generated.
    /// Useful for debugging and auditing.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Factory method to create a successful API response.
    /// </summary>
    /// <param name="data">The response data.</param>
    /// <param name="message">Optional message describing the success.</param>
    /// <returns>A successful API response.</returns>
    public static ApiResponse<T> SuccessResponse(T data, string message = "Operation completed successfully")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Errors = new List<string>(),
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Factory method to create a failed API response with a single error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="error">Additional error detail.</param>
    /// <returns>A failed API response.</returns>
    public static ApiResponse<T> FailureResponse(string message, string? error = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default,
            Message = message,
            Errors = error != null ? new List<string> { error } : new List<string>(),
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Factory method to create a failed API response with multiple errors.
    /// </summary>
    /// <param name="message">The primary error message.</param>
    /// <param name="errors">Additional error details.</param>
    /// <returns>A failed API response.</returns>
    public static ApiResponse<T> FailureResponse(string message, List<string> errors)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default,
            Message = message,
            Errors = errors,
            Timestamp = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Non-generic variant of ApiResponse for endpoints that don't return data.
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// Indicates whether the API call was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Human-readable message describing the result.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// List of errors that occurred during the operation.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// UTC timestamp when the response was generated.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Factory method to create a successful API response.
    /// </summary>
    /// <param name="message">Message describing the success.</param>
    /// <returns>A successful API response.</returns>
    public static ApiResponse SuccessResponse(string message = "Operation completed successfully")
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            Errors = new List<string>(),
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Factory method to create a failed API response.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="error">Additional error detail.</param>
    /// <returns>A failed API response.</returns>
    public static ApiResponse FailureResponse(string message, string? error = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = error != null ? new List<string> { error } : new List<string>(),
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Factory method to create a failed API response with multiple errors.
    /// </summary>
    /// <param name="message">The primary error message.</param>
    /// <param name="errors">Additional error details.</param>
    /// <returns>A failed API response.</returns>
    public static ApiResponse FailureResponse(string message, List<string> errors)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors,
            Timestamp = DateTime.UtcNow
        };
    }
}
