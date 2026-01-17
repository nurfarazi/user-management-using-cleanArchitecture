using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserManagement.Shared.Contracts.Services;
using UserManagement.Shared.Models.DTOs;
using UserManagement.Shared.Models.Results;

namespace UserManagement.API.Controllers;

/// <summary>
/// API controller for user management endpoints.
/// Handles HTTP requests related to user operations.
/// This controller has no business logic; it delegates to IUserService.
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Produces("application/json")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    /// <summary>
    /// Initializes a new instance of the UserController class.
    /// </summary>
    /// <param name="userService">Service for user operations.</param>
    /// <param name="logger">Logger for controller operations.</param>
    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a new user in the system.
    /// </summary>
    /// <param name="request">The registration request containing user details.</param>
    /// <returns>
    /// 201 Created if registration is successful with the new user details.
    /// 400 Bad Request if validation fails.
    /// 409 Conflict if the email already exists.
    /// 500 Internal Server Error if an unexpected error occurs.
    /// </returns>
    /// <response code="201">User registered successfully.</response>
    /// <response code="400">Validation failed - check error details.</response>
    /// <response code="409">Email already exists in the system.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterUserRequest request)
    {
        try
        {
            _logger.LogInformation("Registration request received for email: {Email}", request?.Email);

            if (request != null)
            {
                request.RegistrationIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                request.RegistrationUserAgent = Request.Headers["User-Agent"].ToString();
            }

            // Call service layer
            var result = await _userService.RegisterUserAsync(request!);

            // Handle success
            if (result.IsSuccess)
            {
                _logger.LogInformation("User registered successfully for email: {Email}", request?.Email);

                var response = ApiResponse<RegisterUserResponse>.SuccessResponse(
                    result.Value!,
                    "User registered successfully");

                // Return 201 Created with Location header
                return CreatedAtAction(
                    nameof(RegisterUser),
                    new { userId = result.Value!.UserId },
                    response);
            }

            // Handle failure - email already exists
            if (result.ErrorCode == "EMAIL_ALREADY_EXISTS")
            {
                _logger.LogWarning("Registration failed: Email already exists: {Email}", request?.Email);

                var response = ApiResponse<RegisterUserResponse>.FailureResponse(
                    "Email already exists",
                    result.Errors);

                return Conflict(response);
            }

            // Handle failure - phone number already exists
            if (result.ErrorCode == "PHONE_ALREADY_EXISTS")
            {
                _logger.LogWarning("Registration failed: Phone number already exists: {PhoneNumber}", request?.PhoneNumber);

                var response = ApiResponse<RegisterUserResponse>.FailureResponse(
                    "Phone number already exists",
                    result.Errors);

                return Conflict(response);
            }

            // Handle other business logic failures
            _logger.LogWarning("Registration failed for email {Email}: {Error}", request?.Email, result.ErrorMessage);

            var failureResponse = ApiResponse<RegisterUserResponse>.FailureResponse(
                result.ErrorMessage ?? "Registration failed",
                result.Errors);

            return BadRequest(failureResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during user registration for email: {Email}", request?.Email);

            var errorResponse = ApiResponse.FailureResponse(
                "An unexpected error occurred during registration",
                "REGISTRATION_ERROR");

            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// Updates an existing user's profile information.
    /// Users can only update their own profile; admins can update any user.
    /// </summary>
    /// <param name="userId">The ID of the user to update.</param>
    /// <param name="request">The update request containing new user details.</param>
    /// <returns>
    /// 200 OK if update is successful with the updated user details.
    /// 401 Unauthorized if not authenticated.
    /// 403 Forbidden if user tries to update another user's profile (and is not admin).
    /// 404 Not Found if the user ID does not exist.
    /// 400 Bad Request if validation fails.
    /// 409 Conflict if business rules like phone uniqueness are violated.
    /// </returns>
    [HttpPut("{userId}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UpdateUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateUser([FromRoute] string userId, [FromBody] UpdateUserRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest(ApiResponse.FailureResponse("Update request is required"));

            // Ownership validation
            if (!IsOwnerOrAdmin(userId))
            {
                _logger.LogWarning("Unauthorized update attempt: User {CurrentUserId} tried to update user {TargetUserId}", 
                    User.FindFirstValue(ClaimTypes.NameIdentifier), userId);
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.FailureResponse("You are not authorized to update this profile"));
            }

            // Ensure the ID in route matches ID in body (or just use route ID)
            request.Id = userId;

            _logger.LogInformation("Update request received for user ID: {UserId}", userId);

            // Call service layer
            var result = await _userService.UpdateUserAsync(request);

            // Handle success
            if (result.IsSuccess)
            {
                _logger.LogInformation("User updated successfully: {UserId}", userId);

                var response = ApiResponse<UpdateUserResponse>.SuccessResponse(
                    result.Value!,
                    "User updated successfully");

                return Ok(response);
            }

            // Handle failure - not found
            if (result.ErrorCode == "USER_NOT_FOUND")
            {
                _logger.LogWarning("Update failed: User with ID {UserId} not found", userId);
                return NotFound(ApiResponse.FailureResponse(result.ErrorMessage ?? "User not found"));
            }

            // Handle failure - uniqueness conflict (e.g. phone)
            if (result.ErrorCode == "PHONE_ALREADY_EXISTS")
            {
                _logger.LogWarning("Update failed: Phone number already exists for user {UserId}", userId);
                return Conflict(ApiResponse.FailureResponse(result.ErrorMessage ?? "Phone number already exists", result.Errors));
            }

            // Handle other business logic failures
            _logger.LogWarning("Update failed for user {UserId}: {Error}", userId, result.ErrorMessage);

            var failureResponse = ApiResponse<UpdateUserResponse>.FailureResponse(
                result.ErrorMessage ?? "Update failed",
                result.Errors);

            return BadRequest(failureResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during user update for ID: {UserId}", userId);

            var errorResponse = ApiResponse.FailureResponse(
                "An unexpected error occurred during update",
                "UPDATE_ERROR");

            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// Retrieves a single user by ID.
    /// Users can view their own profile; admins can view any user profile.
    /// </summary>
    /// <param name="userId">The ID of the user to retrieve.</param>
    /// <returns>
    /// 200 OK with user details if found.
    /// 401 Unauthorized if not authenticated.
    /// 403 Forbidden if user tries to view another user's profile (and is not admin).
    /// 404 Not Found if user does not exist.
    /// </returns>
    /// <response code="200">User retrieved successfully.</response>
    /// <response code="401">Not authenticated or invalid access token.</response>
    /// <response code="403">Insufficient permissions to view this user.</response>
    /// <response code="404">User not found.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpGet("{userId}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUser([FromRoute] string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(ApiResponse.FailureResponse("User ID is required"));

            // Ownership validation
            if (!IsOwnerOrAdmin(userId))
            {
                _logger.LogWarning("Unauthorized access attempt: User {CurrentUserId} tried to view user {TargetUserId}", 
                    User.FindFirstValue(ClaimTypes.NameIdentifier), userId);
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.FailureResponse("You are not authorized to view this profile"));
            }

            _logger.LogInformation("Retrieving user: {UserId}", userId);

            var result = await _userService.GetUserByIdAsync(userId);

            if (result.IsSuccess)
            {
                _logger.LogInformation("User retrieved successfully: {UserId}", userId);

                var response = ApiResponse<UserDto>.SuccessResponse(
                    result.Value!,
                    "User retrieved successfully");

                return Ok(response);
            }

            // User not found
            if (result.ErrorCode == "USER_NOT_FOUND")
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return NotFound(ApiResponse.FailureResponse(result.ErrorMessage ?? "User not found"));
            }

            _logger.LogWarning("Failed to retrieve user {UserId}: {Error}", userId, result.ErrorMessage);

            var failureResponse = ApiResponse.FailureResponse(
                result.ErrorMessage ?? "Failed to retrieve user",
                result.Errors);

            return BadRequest(failureResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving user: {UserId}", userId);

            var errorResponse = ApiResponse.FailureResponse(
                "An unexpected error occurred while retrieving user",
                "RETRIEVAL_ERROR");

            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// Retrieves a paginated list of all users with optional filtering and sorting.
    /// Only admins can access this endpoint.
    /// </summary>
    /// <param name="request">Pagination, filtering, and sorting parameters.</param>
    /// <returns>
    /// 200 OK with paged list of users.
    /// 401 Unauthorized if not authenticated.
    /// 403 Forbidden if user does not have admin role.
    /// 400 Bad Request if validation fails.
    /// </returns>
    /// <response code="200">Users retrieved successfully.</response>
    /// <response code="401">Not authenticated or invalid access token.</response>
    /// <response code="403">Admin role required.</response>
    /// <response code="400">Validation failed - check error details.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUsers([FromQuery] GetUsersRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest(ApiResponse.FailureResponse("Request is required"));

            _logger.LogInformation("Retrieving users: Page {PageNumber}, Size {PageSize}",
                request.PageNumber, request.PageSize);

            var result = await _userService.GetUsersAsync(request);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Users retrieved successfully. Retrieved {Count} out of {Total}",
                    result.Value!.Items.Count(), result.Value.TotalCount);

                var response = ApiResponse<PagedResult<UserDto>>.SuccessResponse(
                    result.Value,
                    "Users retrieved successfully");

                return Ok(response);
            }

            _logger.LogWarning("Failed to retrieve users: {Error}", result.ErrorMessage);

            var failureResponse = ApiResponse.FailureResponse(
                result.ErrorMessage ?? "Failed to retrieve users",
                result.Errors);

            return BadRequest(failureResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving users");

            var errorResponse = ApiResponse.FailureResponse(
                "An unexpected error occurred while retrieving users",
                "RETRIEVAL_ERROR");

            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// Soft deletes a user (marks as deleted without physical removal).
    /// Users can only delete their own profile; admins can delete any user.
    /// After deletion, the user cannot log in.
    /// </summary>
    /// <param name="userId">The ID of the user to delete.</param>
    /// <returns>
    /// 200 OK if deletion is successful.
    /// 401 Unauthorized if not authenticated.
    /// 403 Forbidden if user tries to delete another user's profile (and is not admin).
    /// 404 Not Found if user does not exist.
    /// </returns>
    /// <response code="200">User deleted successfully.</response>
    /// <response code="401">Not authenticated or invalid access token.</response>
    /// <response code="403">Insufficient permissions to delete this user.</response>
    /// <response code="404">User not found.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpDelete("{userId}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteUser([FromRoute] string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(ApiResponse.FailureResponse("User ID is required"));

            // Ownership validation
            if (!IsOwnerOrAdmin(userId))
            {
                _logger.LogWarning("Unauthorized deletion attempt: User {CurrentUserId} tried to delete user {TargetUserId}", 
                    User.FindFirstValue(ClaimTypes.NameIdentifier), userId);
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.FailureResponse("You are not authorized to delete this profile"));
            }

            _logger.LogInformation("Deleting user: {UserId}", userId);

            var result = await _userService.SoftDeleteUserAsync(userId);

            if (result.IsSuccess)
            {
                _logger.LogInformation("User deleted successfully: {UserId}", userId);

                var response = ApiResponse.SuccessResponse(
                    "User deleted successfully");

                return Ok(response);
            }

            // User not found
            if (result.ErrorCode == "USER_NOT_FOUND")
            {
                _logger.LogWarning("User not found for deletion: {UserId}", userId);
                return NotFound(ApiResponse.FailureResponse(result.ErrorMessage ?? "User not found"));
            }

            _logger.LogWarning("Failed to delete user {UserId}: {Error}", userId, result.ErrorMessage);

            var failureResponse = ApiResponse.FailureResponse(
                result.ErrorMessage ?? "Failed to delete user",
                result.Errors);

            return BadRequest(failureResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting user: {UserId}", userId);

            var errorResponse = ApiResponse.FailureResponse(
                "An unexpected error occurred while deleting user",
                "DELETION_ERROR");

            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// Changes the password for the current user.
    /// </summary>
    [HttpPost("{userId}/change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromRoute] string userId, [FromBody] ChangePasswordRequest request)
    {
        if (userId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.FailureResponse("You can only change your own password"));

        var result = await _userService.ChangePasswordAsync(userId, request);
        return result.IsSuccess ? Ok(ApiResponse.SuccessResponse("Password changed successfully")) : BadRequest(ApiResponse.FailureResponse(result.ErrorMessage ?? "Failed to change password"));
    }

    /// <summary>
    /// Updates a user's account status. Only admins can access this.
    /// </summary>
    [HttpPatch("{userId}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus([FromRoute] string userId, [FromBody] UpdateUserStatusRequest request)
    {
        var result = await _userService.UpdateUserStatusAsync(userId, request);
        return result.IsSuccess ? Ok(ApiResponse.SuccessResponse("Status updated successfully")) : BadRequest(ApiResponse.FailureResponse(result.ErrorMessage ?? "Failed to update status"));
    }

    private bool IsOwnerOrAdmin(string userId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = User.FindFirstValue(ClaimTypes.Role);
        return currentUserId == userId || userRole == "Admin";
    }
}
