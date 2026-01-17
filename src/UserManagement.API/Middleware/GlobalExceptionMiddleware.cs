using System.Net;
using FluentValidation;
using UserManagement.Shared.Exceptions;
using UserManagement.Shared.Models.Results;

namespace UserManagement.API.Middleware;

/// <summary>
/// Global exception handling middleware for ASP.NET Core.
/// Catches all unhandled exceptions and returns consistent error responses.
/// Maps domain exceptions to appropriate HTTP status codes.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the GlobalExceptionMiddleware class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">Logger for middleware operations.</param>
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to handle the HTTP request and any exceptions.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Handles exceptions by mapping them to appropriate HTTP responses.
    /// </summary>
    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            // Validation exceptions -> 400 Bad Request
            ValidationException validationEx => HandleValidationException(context, validationEx),

            // Domain-specific exceptions
            UserAlreadyExistsException userExistsEx => HandleUserAlreadyExistsException(context, userExistsEx),
            ForbiddenException forbiddenEx => HandleForbiddenException(context, forbiddenEx),
            InvalidTokenException tokenEx => HandleInvalidTokenException(context, tokenEx),

            // Generic exception -> 500 Internal Server Error
            _ => HandleGenericException(context, exception)
        };

        return context.Response.WriteAsJsonAsync(response);
    }

    /// <summary>
    /// Handles FluentValidation exceptions.
    /// </summary>
    private ApiResponse HandleValidationException(HttpContext context, ValidationException ex)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

        var errors = ex.Errors
            .Select(f => $"{f.PropertyName}: {f.ErrorMessage}")
            .ToList();

        // Use the overload that takes message and error list
        var response = new ApiResponse
        {
            Success = false,
            Message = "Validation failed",
            Errors = errors,
            Timestamp = DateTime.UtcNow
        };

        return response;
    }

    /// <summary>
    /// Handles UserAlreadyExistsException (domain exception).
    /// </summary>
    private ApiResponse HandleUserAlreadyExistsException(HttpContext context, UserAlreadyExistsException ex)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Conflict;
        return ApiResponse.FailureResponse(
            "Email already exists",
            $"A user with email '{ex.Email}' is already registered in the system");
    }

    /// <summary>
    /// Handles ForbiddenException (authorization/permission denied).
    /// </summary>
    private ApiResponse HandleForbiddenException(HttpContext context, ForbiddenException ex)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        return ApiResponse.FailureResponse(
            ex.Message ?? "You do not have permission to access this resource",
            "FORBIDDEN");
    }

    /// <summary>
    /// Handles InvalidTokenException (authentication/invalid token).
    /// </summary>
    private ApiResponse HandleInvalidTokenException(HttpContext context, InvalidTokenException ex)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        return ApiResponse.FailureResponse(
            ex.Message ?? "Invalid or expired authentication token",
            "INVALID_TOKEN");
    }

    /// <summary>
    /// Handles generic exceptions.
    /// </summary>
    private ApiResponse HandleGenericException(HttpContext context, Exception ex)
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        // Don't expose internal exception details in production
        var message = "An unexpected error occurred. Please contact support.";
        if (!IsProduction(context))
            message = ex.Message;

        return ApiResponse.FailureResponse(message);
    }

    /// <summary>
    /// Determines if the application is running in production.
    /// </summary>
    private bool IsProduction(HttpContext context)
    {
        var environment = context.RequestServices.GetRequiredService<IHostEnvironment>();
        return environment.IsProduction();
    }
}
