namespace UserManagement.Shared.Models.Results;

/// <summary>
/// Non-generic result pattern for service layer operations.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public List<string> Errors { get; }
    public string? ErrorCode { get; }

    protected Result(bool isSuccess, string? errorMessage, List<string>? errors = null, string? errorCode = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Errors = errors ?? new List<string>();
        ErrorCode = errorCode;
    }

    public static Result Success() => new(true, null, null, null);

    public static Result Failure(string errorMessage, string? errorCode = null) =>
        new(false, errorMessage, new List<string> { errorMessage }, errorCode);

    public static Result Failure(string errorMessage, List<string> errors, string? errorCode = null) =>
        new(false, errorMessage, errors, errorCode);

    public static Result Failure(Exception exception, string? errorCode = null) =>
        new(false, exception.Message, new List<string> { exception.ToString() }, errorCode ?? "EXCEPTION");
}

/// <summary>
/// Generic result pattern for service layer operations.
/// Encapsulates success or failure of business operations without using exceptions for flow control.
/// This pattern is commonly used in domain-driven design to explicitly represent operation outcomes.
/// </summary>
/// <typeparam name="T">The type of the value returned on success.</typeparam>
public class Result<T>
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// The value returned by the operation if successful; null if failed.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// The error message if the operation failed; null if successful.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Additional error details for complex failure scenarios.
    /// </summary>
    public List<string> Errors { get; }

    /// <summary>
    /// Error code for programmatic error handling and routing.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Private constructor to enforce factory method usage.
    /// </summary>
    private Result(bool isSuccess, T? value, string? errorMessage, List<string>? errors = null, string? errorCode = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
        Errors = errors ?? new List<string>();
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Factory method to create a successful result.
    /// </summary>
    /// <param name="value">The value to return.</param>
    /// <returns>A successful result containing the value.</returns>
    public static Result<T> Success(T value) =>
        new(true, value, null, null, null);

    /// <summary>
    /// Factory method to create a failed result with a single error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <param name="errorCode">Optional error code for categorization.</param>
    /// <returns>A failed result with the error message.</returns>
    public static Result<T> Failure(string errorMessage, string? errorCode = null) =>
        new(false, default, errorMessage, new List<string> { errorMessage }, errorCode);

    /// <summary>
    /// Factory method to create a failed result with multiple error messages.
    /// </summary>
    /// <param name="errorMessage">The primary error message.</param>
    /// <param name="errors">Additional error details.</param>
    /// <param name="errorCode">Optional error code for categorization.</param>
    /// <returns>A failed result with multiple error messages.</returns>
    public static Result<T> Failure(string errorMessage, List<string> errors, string? errorCode = null) =>
        new(false, default, errorMessage, errors, errorCode);

    /// <summary>
    /// Factory method to create a failed result from an exception.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="errorCode">Optional error code for categorization.</param>
    /// <returns>A failed result containing exception details.</returns>
    public static Result<T> Failure(Exception exception, string? errorCode = null) =>
        new(false, default, exception.Message, new List<string> { exception.ToString() }, errorCode ?? "EXCEPTION");

    /// <summary>
    /// Executes a transform function on the value if successful; otherwise returns this failed result.
    /// Useful for chaining operations.
    /// </summary>
    /// <typeparam name="TNew">The type of the new result value.</typeparam>
    /// <param name="transform">Function to transform the value.</param>
    /// <returns>A new result with the transformed value, or this failed result.</returns>
    public Result<TNew> Map<TNew>(Func<T?, TNew> transform)
    {
        if (!IsSuccess)
            return Result<TNew>.Failure(ErrorMessage ?? "Unknown error", Errors, ErrorCode);

        var transformedValue = transform(Value);
        return Result<TNew>.Success(transformedValue);
    }

    /// <summary>
    /// Executes an async transform function on the value if successful; otherwise returns this failed result.
    /// Useful for chaining async operations.
    /// </summary>
    /// <typeparam name="TNew">The type of the new result value.</typeparam>
    /// <param name="transform">Async function to transform the value.</param>
    /// <returns>A task representing the new result with the transformed value, or this failed result.</returns>
    public async Task<Result<TNew>> MapAsync<TNew>(Func<T?, Task<TNew>> transform)
    {
        if (!IsSuccess)
            return Result<TNew>.Failure(ErrorMessage ?? "Unknown error", Errors, ErrorCode);

        var transformedValue = await transform(Value);
        return Result<TNew>.Success(transformedValue);
    }
}
