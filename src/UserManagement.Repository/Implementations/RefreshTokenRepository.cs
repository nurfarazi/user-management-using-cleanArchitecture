using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using UserManagement.Shared.Contracts.Repositories;
using UserManagement.Shared.Models.Entities;

namespace UserManagement.Repository.Implementations;

/// <summary>
/// MongoDB repository implementation for RefreshToken entities.
/// Manages refresh token storage, revocation, and cleanup.
/// </summary>
public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
{
    /// <summary>
    /// Initializes a new instance of the RefreshTokenRepository class.
    /// Creates indexes on application startup to optimize token queries.
    /// </summary>
    /// <param name="database">The MongoDB database instance.</param>
    /// <param name="logger">Logger for repository operations.</param>
    public RefreshTokenRepository(IMongoDatabase database, ILogger<RefreshTokenRepository> logger)
        : base(database, logger)
    {
        // Create indexes on startup
        EnsureIndexesAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Retrieves a refresh token by its token value.
    /// </summary>
    /// <param name="token">The refresh token string value.</param>
    /// <returns>The refresh token entity if found; null otherwise.</returns>
    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                Logger.LogWarning("Attempted to retrieve refresh token with null or empty token");
                return null;
            }

            Logger.LogInformation("Searching for refresh token");
            var filter = Builders<RefreshToken>.Filter.Eq(rt => rt.Token, token);
            var refreshToken = await Collection.Find(filter).FirstOrDefaultAsync();

            if (refreshToken != null)
                Logger.LogInformation("Refresh token found");
            else
                Logger.LogInformation("Refresh token not found");

            return refreshToken;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving refresh token");
            throw;
        }
    }

    /// <summary>
    /// Retrieves all active (non-revoked and non-expired) refresh tokens for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>Collection of active refresh tokens for the user.</returns>
    public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                Logger.LogWarning("Attempted to retrieve active tokens with null or empty user ID");
                return new List<RefreshToken>();
            }

            Logger.LogInformation("Searching for active refresh tokens for user: {UserId}", userId);

            var filter = Builders<RefreshToken>.Filter.And(
                Builders<RefreshToken>.Filter.Eq(rt => rt.UserId, userId),
                Builders<RefreshToken>.Filter.Eq(rt => rt.IsRevoked, false),
                Builders<RefreshToken>.Filter.Gt(rt => rt.ExpiresAt, DateTime.UtcNow)
            );

            var tokens = await Collection.Find(filter).ToListAsync();
            Logger.LogInformation("Found {Count} active refresh tokens for user: {UserId}", tokens.Count, userId);

            return tokens;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving active refresh tokens for user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Revokes a single refresh token by setting IsRevoked = true.
    /// </summary>
    /// <param name="tokenId">The ID of the token to revoke.</param>
    /// <returns>True if revocation was successful; false if token not found.</returns>
    public async Task<bool> RevokeTokenAsync(string tokenId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tokenId))
            {
                Logger.LogWarning("Attempted to revoke token with null or empty ID");
                return false;
            }

            Logger.LogInformation("Revoking refresh token: {TokenId}", tokenId);

            var filter = Builders<RefreshToken>.Filter.Eq(rt => rt.Id, tokenId);
            var update = Builders<RefreshToken>.Update
                .Set(rt => rt.IsRevoked, true)
                .Set(rt => rt.RevokedAt, DateTime.UtcNow);

            var result = await Collection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount > 0)
                Logger.LogInformation("Refresh token revoked successfully: {TokenId}", tokenId);
            else
                Logger.LogWarning("Refresh token not found for revocation: {TokenId}", tokenId);

            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error revoking refresh token: {TokenId}", tokenId);
            throw;
        }
    }

    /// <summary>
    /// Revokes all refresh tokens for a user (e.g., for logout-all functionality).
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>Number of tokens revoked.</returns>
    public async Task<int> RevokeAllUserTokensAsync(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                Logger.LogWarning("Attempted to revoke tokens with null or empty user ID");
                return 0;
            }

            Logger.LogInformation("Revoking all refresh tokens for user: {UserId}", userId);

            var filter = Builders<RefreshToken>.Filter.And(
                Builders<RefreshToken>.Filter.Eq(rt => rt.UserId, userId),
                Builders<RefreshToken>.Filter.Eq(rt => rt.IsRevoked, false)
            );

            var update = Builders<RefreshToken>.Update
                .Set(rt => rt.IsRevoked, true)
                .Set(rt => rt.RevokedAt, DateTime.UtcNow);

            var result = await Collection.UpdateManyAsync(filter, update);

            Logger.LogInformation("Revoked {Count} refresh tokens for user: {UserId}", result.ModifiedCount, userId);
            return (int)result.ModifiedCount;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error revoking all tokens for user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Cleans up expired tokens from the database (maintenance operation).
    /// </summary>
    /// <returns>Number of tokens deleted.</returns>
    public async Task<int> DeleteExpiredTokensAsync()
    {
        try
        {
            Logger.LogInformation("Cleaning up expired refresh tokens");

            var filter = Builders<RefreshToken>.Filter.Lt(rt => rt.ExpiresAt, DateTime.UtcNow);
            var result = await Collection.DeleteManyAsync(filter);

            Logger.LogInformation("Deleted {Count} expired refresh tokens", result.DeletedCount);
            return (int)result.DeletedCount;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting expired refresh tokens");
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
            Logger.LogInformation("Creating indexes for RefreshToken collection");

            // Unique index on token field
            var tokenIndexModel = new CreateIndexModel<RefreshToken>(
                Builders<RefreshToken>.IndexKeys.Ascending(rt => rt.Token),
                new CreateIndexOptions { Unique = true }
            );

            // Index on userId for finding tokens by user
            var userIdIndexModel = new CreateIndexModel<RefreshToken>(
                Builders<RefreshToken>.IndexKeys.Ascending(rt => rt.UserId)
            );

            // Index on expiresAt for cleanup operations
            var expiresAtIndexModel = new CreateIndexModel<RefreshToken>(
                Builders<RefreshToken>.IndexKeys.Ascending(rt => rt.ExpiresAt)
            );

            // Compound index on userId and isRevoked for active tokens query
            var userIdRevokedIndexModel = new CreateIndexModel<RefreshToken>(
                Builders<RefreshToken>.IndexKeys
                    .Ascending(rt => rt.UserId)
                    .Ascending(rt => rt.IsRevoked)
            );

            await Collection.Indexes.CreateManyAsync(new[]
            {
                tokenIndexModel,
                userIdIndexModel,
                expiresAtIndexModel,
                userIdRevokedIndexModel
            });

            Logger.LogInformation("Indexes created successfully for RefreshToken collection");
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict" || ex.CodeName == "DuplicateKey")
        {
            // Index already exists, this is fine
            Logger.LogInformation("Index already exists for RefreshToken collection");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating indexes for RefreshToken collection");
            throw;
        }
    }
}
