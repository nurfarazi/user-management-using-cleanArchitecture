using UserManagement.Shared.Models.DTOs;
using UserManagement.Shared.Models.Entities;

namespace UserManagement.Shared.Contracts.Repositories;

/// <summary>
/// Repository interface for User entities.
/// Extends the generic IRepository with user-specific data access methods.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Retrieves a user by their email address.
    /// Email is a unique identifier in the system and commonly used for lookups.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <returns>The user if found; null otherwise.</returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Checks if a user with the given email already exists.
    /// Used for validation during registration to ensure email uniqueness.
    /// Includes both active and soft-deleted users.
    /// </summary>
    /// <param name="email">The email address to check for existence.</param>
    /// <returns>True if a user with this email exists; false otherwise.</returns>
    Task<bool> EmailExistsAsync(string email);

    /// <summary>
    /// Checks if a user with the given phone number already exists.
    /// Used for validation during registration to ensure phone number uniqueness.
    /// Includes both active and soft-deleted users.
    /// </summary>
    /// <param name="phoneNumber">The phone number to check for existence.</param>
    /// <returns>True if a user with this phone number exists; false otherwise.</returns>
    Task<bool> PhoneNumberExistsAsync(string phoneNumber);

    /// <summary>
    /// Retrieves a paginated list of users with filtering and sorting support.
    /// </summary>
    /// <param name="request">Pagination, filtering, and sorting parameters.</param>
    /// <returns>Paged result containing users and pagination metadata.</returns>
    Task<PagedResult<User>> GetPagedAsync(GetUsersRequest request);

    /// <summary>
    /// Soft deletes a user by setting IsDeleted = true.
    /// The user record is not physically deleted from the database.
    /// </summary>
    /// <param name="userId">The ID of the user to soft delete.</param>
    /// <returns>True if soft deletion was successful; false if user not found.</returns>
    Task<bool> SoftDeleteAsync(string userId);
}
