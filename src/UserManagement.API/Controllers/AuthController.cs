using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Shared.Contracts.Repositories;
using UserManagement.Shared.Contracts.Services;
using UserManagement.Shared.Models.DTOs;
using UserManagement.Shared.Models.Results;

namespace UserManagement.API.Controllers;

/// <summary>
/// API controller for authentication and token management endpoints.
/// Handles user login, token refresh, and token revocation.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Initializes a new instance of the AuthController class.
    /// </summary>
    public AuthController(
        IAuthenticationService authService,
        IJwtTokenService jwtTokenService,
        ILogger<AuthController> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Authenticates a user with email and password, returning access and refresh tokens.
    /// </summary>
    /// <param name="request">The login request containing email and password.</param>
    /// <returns>
    /// 200 OK with access token, refresh token, and user information if authentication succeeds.
    /// 401 Unauthorized if credentials are invalid.
    /// 400 Bad Request if validation fails.
    /// </returns>
    /// <response code="200">User authenticated successfully.</response>
    /// <response code="401">Invalid email or password.</response>
    /// <response code="400">Validation failed - check error details.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Login request received for email: {Email}", request?.Email);

            var result = await _authService.AuthenticateAsync(
                request!.Email,
                request.Password);

            if (result.IsSuccess)
            {
                _logger.LogInformation("User logged in successfully: {Email}", request.Email);

                var response = ApiResponse<LoginResponse>.SuccessResponse(
                    result.Value!,
                    "Authentication successful");

                return Ok(response);
            }

            // Handle specific failure cases
            if (result.ErrorCode == "INVALID_CREDENTIALS" || result.ErrorCode == "USER_DELETED")
            {
                _logger.LogWarning("Login failed: {ErrorCode} - {ErrorMessage}", result.ErrorCode, result.ErrorMessage);

                var response = ApiResponse.FailureResponse(
                    result.ErrorMessage ?? "Invalid credentials",
                    result.Errors);

                return Unauthorized(response);
            }

            // Handle other failures
            _logger.LogWarning("Login failed for email {Email}: {Error}", request?.Email, result.ErrorMessage);

            var failureResponse = ApiResponse.FailureResponse(
                result.ErrorMessage ?? "Authentication failed",
                result.Errors);

            return BadRequest(failureResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for email: {Email}", request?.Email);

            var errorResponse = ApiResponse.FailureResponse(
                "An unexpected error occurred during authentication",
                "LOGIN_ERROR");

            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// Issues a new access token and rotates the refresh token (revokes old one).
    /// </summary>
    /// <param name="request">The refresh token request.</param>
    /// <returns>
    /// 200 OK with new access token and refresh token if refresh succeeds.
    /// 401 Unauthorized if refresh token is invalid or expired.
    /// 400 Bad Request if validation fails.
    /// </returns>
    /// <response code="200">Token refreshed successfully.</response>
    /// <response code="401">Invalid or expired refresh token.</response>
    /// <response code="400">Validation failed - check error details.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            _logger.LogInformation("Refresh token request received");

            var result = await _authService.RefreshTokenAsync(request!.RefreshToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Token refreshed successfully");

                var response = ApiResponse<LoginResponse>.SuccessResponse(
                    result.Value!,
                    "Token refreshed successfully");

                return Ok(response);
            }

            // Handle token-specific failures
            if (result.ErrorCode == "INVALID_REFRESH_TOKEN" || result.ErrorCode == "TOKEN_EXPIRED" || result.ErrorCode == "TOKEN_REVOKED")
            {
                _logger.LogWarning("Token refresh failed: {ErrorCode} - {ErrorMessage}", result.ErrorCode, result.ErrorMessage);

                var response = ApiResponse.FailureResponse(
                    result.ErrorMessage ?? "Invalid refresh token",
                    result.Errors);

                return Unauthorized(response);
            }

            // Handle other failures
            _logger.LogWarning("Token refresh failed: {Error}", result.ErrorMessage);

            var failureResponse = ApiResponse.FailureResponse(
                result.ErrorMessage ?? "Token refresh failed",
                result.Errors);

            return BadRequest(failureResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token refresh");

            var errorResponse = ApiResponse.FailureResponse(
                "An unexpected error occurred during token refresh",
                "REFRESH_ERROR");

            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// Revokes the current refresh token (logout from current session/device).
    /// Requires authentication via access token.
    /// </summary>
    /// <param name="request">The refresh token to revoke.</param>
    /// <returns>
    /// 200 OK if token is successfully revoked.
    /// 401 Unauthorized if not authenticated.
    /// 400 Bad Request if validation fails.
    /// </returns>
    /// <response code="200">Token revoked successfully.</response>
    /// <response code="401">Not authenticated or invalid access token.</response>
    /// <response code="400">Validation failed - check error details.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        try
        {
            _logger.LogInformation("Logout request received");

            var result = await _authService.RevokeTokenAsync(request!.RefreshToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("User logged out successfully");

                var response = ApiResponse.SuccessResponse(
                    "Logged out successfully");

                return Ok(response);
            }

            _logger.LogWarning("Logout failed: {Error}", result.ErrorMessage);

            var failureResponse = ApiResponse.FailureResponse(
                result.ErrorMessage ?? "Logout failed",
                result.Errors);

            return BadRequest(failureResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during logout");

            var errorResponse = ApiResponse.FailureResponse(
                "An unexpected error occurred during logout",
                "LOGOUT_ERROR");

            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// Revokes all refresh tokens for the authenticated user (logout from all sessions/devices).
    /// Requires authentication via access token.
    /// </summary>
    /// <returns>
    /// 200 OK if all tokens are successfully revoked.
    /// 401 Unauthorized if not authenticated.
    /// 400 Bad Request if operation fails.
    /// </returns>
    /// <response code="200">All tokens revoked successfully.</response>
    /// <response code="401">Not authenticated or invalid access token.</response>
    /// <response code="400">Operation failed - check error details.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LogoutAll()
    {
        try
        {
            _logger.LogInformation("Logout all request received");

            // Extract user ID from the current access token
            var token = GetTokenFromRequest();
            var userId = string.IsNullOrEmpty(token) ? null : _jwtTokenService.GetUserIdFromToken(token);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unable to extract user ID from token");
                return Unauthorized(ApiResponse.FailureResponse("Invalid token", "INVALID_TOKEN"));
            }

            var result = await _authService.RevokeAllUserTokensAsync(userId);

            if (result.IsSuccess)
            {
                _logger.LogInformation("User logged out from all sessions: {UserId}", userId);

                var response = ApiResponse.SuccessResponse(
                    "Logged out from all sessions successfully");

                return Ok(response);
            }

            _logger.LogWarning("Logout all failed: {Error}", result.ErrorMessage);

            var failureResponse = ApiResponse.FailureResponse(
                result.ErrorMessage ?? "Logout all failed",
                result.Errors);

            return BadRequest(failureResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during logout all");

            var errorResponse = ApiResponse.FailureResponse(
                "An unexpected error occurred during logout all",
                "LOGOUT_ALL_ERROR");

            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// Extracts the JWT token from the Authorization header.
    /// </summary>
    /// <returns>The JWT token string, or null if not found.</returns>
    private string? GetTokenFromRequest()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader))
            return null;

        const string bearerScheme = "Bearer ";
        if (authHeader.StartsWith(bearerScheme))
            return authHeader[bearerScheme.Length..];

        return null;
    }
}
