using UserManagement.Shared.Models.DTOs;
using UserManagement.Shared.Models.Results;

namespace UserManagement.Shared.Contracts.Services;

/// <summary>
/// Service interface for authentication operations.
/// Coordinates authentication, token refresh, and token revocation with repositories.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with email and password, returning access and refresh tokens.
    /// </summary>
    /// <param name="email">User's email address.</param>
    /// <param name="password">User's plain-text password.</param>
    /// <returns>Result containing login response with tokens, or failure if credentials invalid.</returns>
    Task<Result<LoginResponse>> AuthenticateAsync(string email, string password);

    /// <summary>
    /// Refreshes an access token using a refresh token (with token rotation).
    /// The old refresh token is revoked and a new one is issued.
    /// </summary>
    /// <param name="oldRefreshToken">The refresh token from the client.</param>
    /// <returns>Result containing new tokens, or failure if refresh token invalid.</returns>
    Task<Result<LoginResponse>> RefreshTokenAsync(string oldRefreshToken);

    /// <summary>
    /// Revokes a single refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token to revoke.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> RevokeTokenAsync(string refreshToken);

    /// <summary>
    /// Revokes all refresh tokens for a user (logout all devices/sessions).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> RevokeAllUserTokensAsync(string userId);
}
