using System.Security.Claims;
using UserManagement.Shared.Models.DTOs;
using UserManagement.Shared.Models.Entities;
using UserManagement.Shared.Models.Results;

namespace UserManagement.Shared.Contracts.Services;

/// <summary>
/// Service interface for JWT token operations.
/// Handles token generation, validation, and refresh operations.
/// Note: Repository operations are coordinated at the controller or higher-level service layer.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT access token for a user.
    /// </summary>
    /// <param name="user">The user entity to generate token for.</param>
    /// <returns>JWT access token string.</returns>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generates a cryptographically random refresh token string.
    /// </summary>
    /// <returns>Refresh token string.</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates and parses a JWT token.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>Principal claims if valid; null if invalid or expired.</returns>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Extracts the user ID claim from a JWT token.
    /// </summary>
    /// <param name="token">The JWT token.</param>
    /// <returns>User ID if found; null otherwise.</returns>
    string? GetUserIdFromToken(string token);

    /// <summary>
    /// Extracts the email claim from a JWT token.
    /// </summary>
    /// <param name="token">The JWT token.</param>
    /// <returns>Email if found; null otherwise.</returns>
    string? GetEmailFromToken(string token);
}
