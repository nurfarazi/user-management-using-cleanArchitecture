using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using UserManagement.Shared.Contracts.Repositories;
using UserManagement.Shared.Models.DTOs;
using UserManagement.Shared.Models.Entities;

namespace UserManagement.Repository.Implementations;

/// <summary>
/// MongoDB repository implementation for User entities.
/// Provides user-specific data access methods while inheriting generic CRUD operations from BaseRepository.
/// Handles MongoDB-specific operations for user management.
/// </summary>
public class UserRepository : BaseRepository<User>, IUserRepository
{
    /// <summary>
    /// Initializes a new instance of the UserRepository class.
    /// Creates indexes on application startup to optimize queries.
    /// </summary>
    /// <param name="database">The MongoDB database instance.</param>
    /// <param name="logger">Logger for repository operations.</param>
    public UserRepository(IMongoDatabase database, ILogger<UserRepository> logger)
        : base(database, logger)
    {
        // Create indexes on startup
        EnsureIndexesAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Retrieves a user by their email address.
    /// Email uniqueness is a business requirement, so this is a common lookup.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <returns>The user if found; null otherwise.</returns>
    public async Task<User?> GetByEmailAsync(string email)
    {
        try
        {
            Logger.LogInformation("Searching for user with email: {Email}", email);

            var filter = Builders<User>.Filter.Eq(u => u.Email, email);
            var user = await Collection.Find(filter).FirstOrDefaultAsync();

            if (user != null)
                Logger.LogInformation("User found with email: {Email}", email);
            else
                Logger.LogInformation("User not found with email: {Email}", email);

            return user;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving user by email: {Email}", email);
            throw;
        }
    }

    /// <summary>
    /// Checks if a user with the given email already exists.
    /// Used during registration validation to ensure email uniqueness.
    /// This check is case-insensitive.
    /// </summary>
    /// <param name="email">The email address to check.</param>
    /// <returns>True if a user with this email exists; false otherwise.</returns>
    public async Task<bool> EmailExistsAsync(string email)
    {
        try
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            Logger.LogInformation("Checking if email exists (normalized): {Email}", normalizedEmail);

            var filter = Builders<User>.Filter.Eq(u => u.Email, normalizedEmail);
            var count = await Collection.CountDocumentsAsync(filter);

            var exists = count > 0;
            Logger.LogInformation("Email existence check result for {Email}: {Exists}", normalizedEmail, exists);

            return exists;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking email existence: {Email}", email);
            throw;
        }
    }

    /// <summary>
    /// Checks if a user with the given phone number already exists.
    /// Used during registration validation to ensure phone number uniqueness.
    /// </summary>
    /// <param name="phoneNumber">The phone number to check.</param>
    /// <returns>True if a user with this phone number exists; false otherwise.</returns>
    public async Task<bool> PhoneNumberExistsAsync(string phoneNumber)
    {
        try
        {
            Logger.LogInformation("Checking if phone number exists: {PhoneNumber}", phoneNumber);

            var filter = Builders<User>.Filter.Eq(u => u.PhoneNumber, phoneNumber);
            var count = await Collection.CountDocumentsAsync(filter);

            var exists = count > 0;
            Logger.LogInformation("Phone number existence check result for {PhoneNumber}: {Exists}", phoneNumber, exists);

            return exists;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking phone number existence: {PhoneNumber}", phoneNumber);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a paginated list of users with filtering and sorting support.
    /// </summary>
    /// <param name="request">Pagination, filtering, and sorting parameters.</param>
    /// <returns>Paged result containing users and pagination metadata.</returns>
    public async Task<PagedResult<User>> GetPagedAsync(GetUsersRequest request)
    {
        try
        {
            // Validate request
            if (request == null)
            {
                Logger.LogWarning("GetPagedAsync called with null request");
                return PagedResult<User>.Create(new List<User>(), 0, 1, 10);
            }

            // Ensure pagination parameters are valid
            var pageNumber = Math.Max(1, request.PageNumber);
            var pageSize = Math.Max(1, Math.Min(request.PageSize, 100)); // Cap at 100

            Logger.LogInformation("Retrieving paged users: Page {PageNumber}, Size {PageSize}, Search: {SearchTerm}, Sort: {SortBy} {SortOrder}",
                pageNumber, pageSize, request.SearchTerm, request.SortBy, request.SortOrder);

            // Build filter
            var filters = new List<FilterDefinition<User>>();

            // Exclude deleted users unless explicitly requested
            if (!request.IncludeDeleted)
            {
                filters.Add(Builders<User>.Filter.Eq(u => u.IsDeleted, false));
            }

            // Search filter (by name or email)
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.Trim();
                var searchFilter = Builders<User>.Filter.Or(
                    Builders<User>.Filter.Regex(u => u.Email, $".*{searchTerm}.*"),
                    Builders<User>.Filter.Regex(u => u.FirstName, $".*{searchTerm}.*"),
                    Builders<User>.Filter.Regex(u => u.LastName, $".*{searchTerm}.*")
                );
                filters.Add(searchFilter);
            }

            // Role filter
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                filters.Add(Builders<User>.Filter.Eq(u => u.Role, request.Role));
            }

            // Date range filter
            if (request.CreatedAfter.HasValue)
            {
                filters.Add(Builders<User>.Filter.Gte(u => u.CreatedAt, request.CreatedAfter.Value));
            }

            if (request.CreatedBefore.HasValue)
            {
                filters.Add(Builders<User>.Filter.Lte(u => u.CreatedAt, request.CreatedBefore.Value));
            }

            var combinedFilter = filters.Any()
                ? Builders<User>.Filter.And(filters)
                : Builders<User>.Filter.Empty;

            // Get total count
            var totalCount = (int)await Collection.CountDocumentsAsync(combinedFilter);

            // Build sort order
            SortDefinition<User> sortDefinition = request.SortBy?.ToLowerInvariant() switch
            {
                "email" => request.SortOrder?.ToLowerInvariant() == "asc"
                    ? Builders<User>.Sort.Ascending(u => u.Email)
                    : Builders<User>.Sort.Descending(u => u.Email),
                "firstname" => request.SortOrder?.ToLowerInvariant() == "asc"
                    ? Builders<User>.Sort.Ascending(u => u.FirstName)
                    : Builders<User>.Sort.Descending(u => u.FirstName),
                "lastname" => request.SortOrder?.ToLowerInvariant() == "asc"
                    ? Builders<User>.Sort.Ascending(u => u.LastName)
                    : Builders<User>.Sort.Descending(u => u.LastName),
                "role" => request.SortOrder?.ToLowerInvariant() == "asc"
                    ? Builders<User>.Sort.Ascending(u => u.Role)
                    : Builders<User>.Sort.Descending(u => u.Role),
                _ => request.SortOrder?.ToLowerInvariant() == "asc"
                    ? Builders<User>.Sort.Ascending(u => u.CreatedAt)
                    : Builders<User>.Sort.Descending(u => u.CreatedAt)
            };

            // Get paged results
            var skip = (pageNumber - 1) * pageSize;
            var users = await Collection
                .Find(combinedFilter)
                .Sort(sortDefinition)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();

            Logger.LogInformation("Retrieved {Count} users out of {Total} total", users.Count, totalCount);

            return PagedResult<User>.Create(users, totalCount, pageNumber, pageSize);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving paged users");
            throw;
        }
    }

    /// <summary>
    /// Updates an existing user with optimistic concurrency check.
    /// </summary>
    /// <param name="id">The unique identifier of the user to update.</param>
    /// <param name="user">The user entity with updated values and incremented version.</param>
    /// <returns>True if the update was successful and version matched; false otherwise.</returns>
    public override async Task<bool> UpdateAsync(string id, User user)
    {
        try
        {
            if (!MongoDB.Bson.ObjectId.TryParse(id, out var objectId))
            {
                Logger.LogWarning("Invalid MongoDB ObjectId format during update: {Id}", id);
                return false;
            }

            // Optimistic concurrency: filter by ID AND the previous version
            // The service has already incremented user.Version, so the DB should have user.Version - 1
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq("_id", objectId),
                Builders<User>.Filter.Eq(u => u.Version, user.Version - 1)
            );

            var result = await Collection.ReplaceOneAsync(filter, user);

            if (result.MatchedCount == 0)
            {
                Logger.LogWarning("Update failed for user {UserId}: Version mismatch or user not found.", id);
                return false;
            }

            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating user with ID: {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Soft deletes a user by setting IsDeleted = true.
    /// The user record is not physically deleted from the database.
    /// </summary>
    /// <param name="userId">The ID of the user to soft delete.</param>
    /// <returns>True if soft deletion was successful; false if user not found.</returns>
    public async Task<bool> SoftDeleteAsync(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                Logger.LogWarning("SoftDeleteAsync called with null or empty user ID");
                return false;
            }

            Logger.LogInformation("Soft deleting user: {UserId}", userId);

            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Set(u => u.IsDeleted, true)
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            var result = await Collection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount > 0)
                Logger.LogInformation("User soft deleted successfully: {UserId}", userId);
            else
                Logger.LogWarning("User not found for soft deletion: {UserId}", userId);

            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error soft deleting user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Ensures that MongoDB indexes are created for optimal query performance.
    /// Should be called during application startup.
    /// </summary>
    private async Task EnsureIndexesAsync()
    {
        try
        {
            Logger.LogInformation("Creating indexes for User collection");

            // Create unique index on Email field
            var emailIndexModel = new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true }
            );

            // Create unique index on PhoneNumber field (sparse since it's optional)
            var phoneIndexModel = new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.PhoneNumber),
                new CreateIndexOptions { Unique = true, Sparse = true }
            );

            // Compound index on FirstName, LastName for search
            var nameIndexModel = new CreateIndexModel<User>(
                Builders<User>.IndexKeys
                    .Ascending(u => u.FirstName)
                    .Ascending(u => u.LastName)
            );

            // Index on CreatedAt for sorting
            var createdAtIndexModel = new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Descending(u => u.CreatedAt)
            );

            // Index on Role for filtering
            var roleIndexModel = new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Role)
            );

            // Index on IsDeleted for excluding soft-deleted users
            var isDeletedIndexModel = new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.IsDeleted)
            );

            await Collection.Indexes.CreateManyAsync(new[]
            {
                emailIndexModel,
                phoneIndexModel,
                nameIndexModel,
                createdAtIndexModel,
                roleIndexModel,
                isDeletedIndexModel
            });

            Logger.LogInformation("Indexes created successfully for User collection");
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict" || ex.CodeName == "DuplicateKey")
        {
            // Index already exists, this is fine
            Logger.LogInformation("Index already exists for User collection");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating indexes for User collection");
            throw;
        }
    }
}
