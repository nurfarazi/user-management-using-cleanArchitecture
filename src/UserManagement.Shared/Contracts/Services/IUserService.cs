using UserManagement.Shared.Models.DTOs;
using UserManagement.Shared.Models.Results;

namespace UserManagement.Shared.Contracts.Services;

/// <summary>
/// Service interface for user management business logic.
/// Defines the contract for all user-related operations.
/// Implementations handle business logic, validation, and coordination with repositories.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Registers a new user in the system.
    /// Performs business logic including email uniqueness validation and password hashing.
    /// </summary>
    /// <param name="request">The registration request containing user data.</param>
    /// <returns>
    /// A Result containing RegisterUserResponse if successful,
    /// or failure details if email already exists or other business rules are violated.
    /// </returns>
    /// <remarks>
    /// This method is idempotent in terms of error handling - calling it multiple times
    /// with the same email will return a failure result each time rather than creating duplicates.
    /// </remarks>
    Task<Result<RegisterUserResponse>> RegisterUserAsync(RegisterUserRequest request);

    /// <summary>
    /// Updates an existing user's profile information.
    /// Validates business rules and preserves immutable fields (Email, Password, IsDeleted).
    /// </summary>
    /// <param name="request">The update request containing new user data.</param>
    /// <returns>A Result containing UpdateUserResponse if successful.</returns>
    Task<Result<UpdateUserResponse>> UpdateUserAsync(UpdateUserRequest request);

    /// <summary>
    /// Retrieves a single user by ID.
    /// </summary>
    /// <param name="userId">The ID of the user to retrieve.</param>
    /// <returns>A Result containing UserDto if user found, or failure if not found.</returns>
    Task<Result<UserDto>> GetUserByIdAsync(string userId);

    /// <summary>
    /// Retrieves a paginated list of users with filtering and sorting.
    /// Excludes soft-deleted users by default.
    /// </summary>
    /// <param name="request">Pagination, filtering, and sorting parameters.</param>
    /// <returns>A Result containing PagedResult of UserDto.</returns>
    Task<Result<PagedResult<UserDto>>> GetUsersAsync(GetUsersRequest request);

    /// <summary>
    /// Soft deletes a user (marks as deleted without physical removal).
    /// The user can no longer log in after deletion.
    /// </summary>
    /// <param name="userId">The ID of the user to delete.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> SoftDeleteUserAsync(string userId);

    /// <summary>
    /// Changes a user's password.
    /// Validates current password and enforces password history policy.
    /// </summary>
    Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request);

    /// <summary>
    /// Updates a user's account status (e.g., Deactivate, Activate).
    /// </summary>
    Task<Result> UpdateUserStatusAsync(string userId, UpdateUserStatusRequest request);
}
