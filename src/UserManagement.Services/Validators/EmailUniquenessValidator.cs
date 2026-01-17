using Microsoft.Extensions.Logging;
using UserManagement.Shared.Contracts.Repositories;
using UserManagement.Shared.Contracts.Validators;
using UserManagement.Shared.Models.Entities;
using UserManagement.Shared.Models.Results;

namespace UserManagement.Services.Validators;

/// <summary>
/// Validates that a user's email is unique across the system.
/// Checks both active and soft-deleted accounts.
/// </summary>
public class EmailUniquenessValidator : IBusinessValidator<User>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<EmailUniquenessValidator> _logger;

    public EmailUniquenessValidator(IUserRepository userRepository, ILogger<EmailUniquenessValidator> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result> ValidateAsync(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
            return Result.Success(); // Email format should be handled by DTO validation

        var normalizedEmail = user.Email.Trim().ToLowerInvariant();
        _logger.LogInformation("Validating email uniqueness for: {Email}", normalizedEmail);

        // Check if email belongs to another user
        var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail);
        
        // If user exists and it's NOT the user we are currently validating (for updates)
        if (existingUser != null && existingUser.Id != user.Id)
        {
            _logger.LogWarning("Email already exists: {Email}", normalizedEmail);
            return Result.Failure(
                "Email already exists",
                new List<string> { "A user with this email address is already registered" },
                "EMAIL_ALREADY_EXISTS");
        }

        return Result.Success();
    }
}
