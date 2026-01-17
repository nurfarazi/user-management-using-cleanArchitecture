using Microsoft.Extensions.Logging;
using UserManagement.Shared.Contracts.Repositories;
using UserManagement.Shared.Contracts.Services;
using UserManagement.Shared.Models.DTOs;
using UserManagement.Shared.Models.Entities;
using UserManagement.Shared.Models.Results;
using UserManagement.Shared.Utils;

namespace UserManagement.Services.Implementations;

/// <summary>
/// Service implementation for authentication operations.
/// Coordinates JWT token service with user and refresh token repositories.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<AuthenticationService> _logger;

    /// <summary>
    /// Initializes a new instance of the AuthenticationService class.
    /// </summary>
    public AuthenticationService(
        IJwtTokenService jwtTokenService,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<AuthenticationService> logger)
    {
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    public async Task<Result<LoginResponse>> AuthenticateAsync(string email, string password)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Authentication attempt with missing email or password");
                return Result<LoginResponse>.Failure("Email and password are required", "MISSING_CREDENTIALS");
            }

            _logger.LogInformation("Authentication attempt for email: {Email}", email);

            // Normalize email
            var normalizedEmail = EmailNormalizer.Normalize(email);

            // Find user by email
            var user = await _userRepository.GetByEmailAsync(normalizedEmail);
            if (user == null)
            {
                _logger.LogWarning("Authentication failed: User not found with email: {Email}", normalizedEmail);
                return Result<LoginResponse>.Failure("Invalid email or password", "INVALID_CREDENTIALS");
            }

            // Check if user is soft-deleted
            if (user.IsDeleted)
            {
                _logger.LogWarning("Authentication failed: User account is deleted: {Email}", normalizedEmail);
                return Result<LoginResponse>.Failure("User account is not available", "USER_DELETED");
            }

            // Verify password using BCrypt
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                _logger.LogWarning("Authentication failed: Invalid password for user: {Email}", normalizedEmail);
                return Result<LoginResponse>.Failure("Invalid email or password", "INVALID_CREDENTIALS");
            }

            // Check Account Status
            if (user.Status == UserStatus.Deactivated.ToStatusString())
            {
                _logger.LogWarning("Authentication failed: Account is deactivated: {Email}", normalizedEmail);
                return Result<LoginResponse>.Failure("Your account has been deactivated", "USER_DEACTIVATED");
            }

            if (user.Status == UserStatus.Banned.ToStatusString())
            {
                _logger.LogWarning("Authentication failed: Account is banned: {Email}", normalizedEmail);
                return Result<LoginResponse>.Failure("Your account has been permanently banned", "USER_BANNED");
            }

            // Generate tokens
            var accessToken = _jwtTokenService.GenerateAccessToken(user);
            var refreshTokenString = _jwtTokenService.GenerateRefreshToken();

            // Store refresh token in database
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenString,
                ExpiresAt = DateTime.UtcNow.AddDays(7), // Match JwtSettings.RefreshTokenExpirationDays
                CreatedAt = DateTime.UtcNow
            };

            await _refreshTokenRepository.AddAsync(refreshToken);

            // Create response
            var userDto = MapUserToDto(user);
            var loginResponse = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenString,
                TokenType = "Bearer",
                ExpiresIn = 15 * 60, // 15 minutes in seconds (match JwtSettings.AccessTokenExpirationMinutes)
                User = userDto
            };

            _logger.LogInformation("User authenticated successfully: {Email}", normalizedEmail);
            return Result<LoginResponse>.Success(loginResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during authentication for email: {Email}", email);
            return Result<LoginResponse>.Failure(ex, "AUTHENTICATION_ERROR");
        }
    }

    /// <summary>
    /// Refreshes an access token using a refresh token (with token rotation).
    /// </summary>
    public async Task<Result<LoginResponse>> RefreshTokenAsync(string oldRefreshToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(oldRefreshToken))
            {
                _logger.LogWarning("Refresh token attempt with null or empty token");
                return Result<LoginResponse>.Failure("Refresh token is required", "MISSING_REFRESH_TOKEN");
            }

            _logger.LogInformation("Refresh token request received");

            // Find the refresh token in database
            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(oldRefreshToken);
            if (refreshToken == null)
            {
                _logger.LogWarning("Refresh token not found in database");
                return Result<LoginResponse>.Failure("Invalid refresh token", "INVALID_REFRESH_TOKEN");
            }

            // Check if token is revoked or expired
            if (refreshToken.IsRevoked)
            {
                _logger.LogWarning("Refresh token is revoked. User ID: {UserId}", refreshToken.UserId);
                return Result<LoginResponse>.Failure("Refresh token has been revoked", "TOKEN_REVOKED");
            }

            if (DateTime.UtcNow > refreshToken.ExpiresAt)
            {
                _logger.LogWarning("Refresh token is expired. User ID: {UserId}", refreshToken.UserId);
                return Result<LoginResponse>.Failure("Refresh token has expired", "TOKEN_EXPIRED");
            }

            // Get user details
            var user = await _userRepository.GetByIdAsync(refreshToken.UserId);
            if (user == null)
            {
                _logger.LogWarning("User not found for refresh token. User ID: {UserId}", refreshToken.UserId);
                return Result<LoginResponse>.Failure("User not found", "USER_NOT_FOUND");
            }

            if (user.IsDeleted)
            {
                _logger.LogWarning("User account is deleted. User ID: {UserId}", refreshToken.UserId);
                return Result<LoginResponse>.Failure("User account is not available", "USER_DELETED");
            }

            // Revoke old refresh token
            await _refreshTokenRepository.RevokeTokenAsync(refreshToken.Id);

            // Generate new tokens (token rotation)
            var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
            var newRefreshTokenString = _jwtTokenService.GenerateRefreshToken();

            // Store new refresh token
            var newRefreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshTokenString,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            await _refreshTokenRepository.AddAsync(newRefreshToken);

            // Create response
            var userDto = MapUserToDto(user);
            var loginResponse = new LoginResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshTokenString,
                TokenType = "Bearer",
                ExpiresIn = 15 * 60,
                User = userDto
            };

            _logger.LogInformation("Token refreshed successfully for user: {UserId}", user.Id);
            return Result<LoginResponse>.Success(loginResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token refresh");
            return Result<LoginResponse>.Failure(ex, "TOKEN_REFRESH_ERROR");
        }
    }

    /// <summary>
    /// Revokes a single refresh token.
    /// </summary>
    public async Task<Result> RevokeTokenAsync(string refreshToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("Revoke token attempt with null or empty token");
                return Result.Failure("Refresh token is required", "MISSING_REFRESH_TOKEN");
            }

            _logger.LogInformation("Revoking refresh token");

            // Find the token
            var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
            if (token == null)
            {
                _logger.LogWarning("Refresh token not found for revocation");
                return Result.Failure("Invalid refresh token", "INVALID_REFRESH_TOKEN");
            }

            // Revoke the token
            var success = await _refreshTokenRepository.RevokeTokenAsync(token.Id);
            if (success)
            {
                _logger.LogInformation("Refresh token revoked successfully");
                return Result.Success();
            }
            else
            {
                _logger.LogWarning("Failed to revoke refresh token");
                return Result.Failure("Failed to revoke token", "REVOCATION_FAILED");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token revocation");
            return Result.Failure(ex, "TOKEN_REVOCATION_ERROR");
        }
    }

    /// <summary>
    /// Revokes all refresh tokens for a user.
    /// </summary>
    public async Task<Result> RevokeAllUserTokensAsync(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Revoke all tokens attempt with null or empty user ID");
                return Result.Failure("User ID is required", "MISSING_USER_ID");
            }

            _logger.LogInformation("Revoking all refresh tokens for user: {UserId}", userId);

            var revokedCount = await _refreshTokenRepository.RevokeAllUserTokensAsync(userId);
            _logger.LogInformation("Revoked {Count} refresh tokens for user: {UserId}", revokedCount, userId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error revoking all tokens for user: {UserId}", userId);
            return Result.Failure(ex, "REVOKE_ALL_ERROR");
        }
    }

    /// <summary>
    /// Maps a User entity to UserDto (without sensitive data).
    /// </summary>
    private static UserDto MapUserToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            DateOfBirth = user.DateOfBirth,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Version = user.Version
        };
    }
}
