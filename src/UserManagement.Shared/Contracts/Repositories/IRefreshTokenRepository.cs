using UserManagement.Shared.Models.Entities;

namespace UserManagement.Shared.Contracts.Repositories;

/// <summary>
/// Repository interface for managing refresh tokens.
/// Extends generic repository with refresh token-specific operations.
/// </summary>
public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    /// <summary>
    /// Retrieves a refresh token by its token value.
    /// </summary>
    /// <param name="token">The refresh token string value.</param>
    /// <returns>The refresh token entity if found; null otherwise.</returns>
    Task<RefreshToken?> GetByTokenAsync(string token);

    /// <summary>
    /// Retrieves all active (non-revoked and non-expired) refresh tokens for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>Collection of active refresh tokens for the user.</returns>
    Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(string userId);

    /// <summary>
    /// Revokes a single refresh token by setting IsRevoked = true.
    /// </summary>
    /// <param name="tokenId">The ID of the token to revoke.</param>
    /// <returns>True if revocation was successful; false if token not found.</returns>
    Task<bool> RevokeTokenAsync(string tokenId);

    /// <summary>
    /// Revokes all refresh tokens for a user (e.g., for logout-all functionality).
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>Number of tokens revoked.</returns>
    Task<int> RevokeAllUserTokensAsync(string userId);

    /// <summary>
    /// Cleans up expired tokens from the database (maintenance operation).
    /// </summary>
    /// <returns>Number of tokens deleted.</returns>
    Task<int> DeleteExpiredTokensAsync();
}
