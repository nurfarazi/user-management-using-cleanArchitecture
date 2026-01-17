using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UserManagement.Shared.Configuration;
using UserManagement.Shared.Contracts.Repositories;
using UserManagement.Shared.Contracts.Services;
using UserManagement.Shared.Models.DTOs;
using UserManagement.Shared.Models.Entities;
using UserManagement.Shared.Models.Results;

namespace UserManagement.Services.Implementations;

/// <summary>
/// Service implementation for JWT token operations.
/// Handles token generation, validation, refresh, and authentication.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<JwtTokenService> _logger;

    /// <summary>
    /// Initializes a new instance of the JwtTokenService class.
    /// </summary>
    /// <param name="jwtSettings">JWT configuration settings.</param>
    /// <param name="logger">Logger for token service operations.</param>
    public JwtTokenService(IOptions<JwtSettings> jwtSettings, ILogger<JwtTokenService> logger)
    {
        _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Validate JWT settings
        if (string.IsNullOrWhiteSpace(_jwtSettings.Secret))
            throw new InvalidOperationException("JWT Secret is not configured");

        if (string.IsNullOrWhiteSpace(_jwtSettings.Issuer))
            throw new InvalidOperationException("JWT Issuer is not configured");

        if (string.IsNullOrWhiteSpace(_jwtSettings.Audience))
            throw new InvalidOperationException("JWT Audience is not configured");
    }

    /// <summary>
    /// Generates a JWT access token for a user.
    /// </summary>
    public string GenerateAccessToken(User user)
    {
        try
        {
            _logger.LogInformation("Generating access token for user: {Email}", user.Email);

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                signingCredentials: credentials
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenString = tokenHandler.WriteToken(token);

            _logger.LogInformation("Access token generated successfully for user: {Email}", user.Email);
            return tokenString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating access token for user: {Email}", user.Email);
            throw;
        }
    }

    /// <summary>
    /// Generates a cryptographically random refresh token string.
    /// </summary>
    public string GenerateRefreshToken()
    {
        try
        {
            _logger.LogInformation("Generating refresh token");

            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }

            var refreshToken = Convert.ToBase64String(randomNumber);
            _logger.LogInformation("Refresh token generated successfully");

            return refreshToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating refresh token");
            throw;
        }
    }

    /// <summary>
    /// Validates and parses a JWT token.
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Attempted to validate null or empty token");
                return null;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // No clock skew for production
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            _logger.LogInformation("Token validated successfully");

            return principal;
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning(ex, "Token has expired");
            return null;
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogWarning(ex, "Token has invalid signature");
            return null;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Security token validation failed");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating token");
            return null;
        }
    }


    /// <summary>
    /// Extracts the user ID claim from a JWT token.
    /// </summary>
    public string? GetUserIdFromToken(string token)
    {
        try
        {
            var principal = ValidateToken(token);
            if (principal == null)
                return null;

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting user ID from token");
            return null;
        }
    }

    /// <summary>
    /// Extracts the email claim from a JWT token.
    /// </summary>
    public string? GetEmailFromToken(string token)
    {
        try
        {
            var principal = ValidateToken(token);
            if (principal == null)
                return null;

            var emailClaim = principal.FindFirst(ClaimTypes.Email);
            return emailClaim?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting email from token");
            return null;
        }
    }

    /// <summary>
    /// Maps a User entity to UserDto for response.
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
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
