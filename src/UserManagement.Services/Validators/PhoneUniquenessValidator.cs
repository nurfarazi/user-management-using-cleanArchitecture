using Microsoft.Extensions.Logging;
using UserManagement.Shared.Contracts.Repositories;
using UserManagement.Shared.Contracts.Validators;
using UserManagement.Shared.Models.Entities;
using UserManagement.Shared.Models.Results;

namespace UserManagement.Services.Validators;

/// <summary>
/// Validates that a user's phone number is unique across the system if provided.
/// </summary>
public class PhoneUniquenessValidator : IBusinessValidator<User>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<PhoneUniquenessValidator> _logger;

    public PhoneUniquenessValidator(IUserRepository userRepository, ILogger<PhoneUniquenessValidator> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result> ValidateAsync(User user)
    {
        if (string.IsNullOrWhiteSpace(user.PhoneNumber))
            return Result.Success();

        _logger.LogInformation("Validating phone number uniqueness for: {PhoneNumber}", user.PhoneNumber);

        // For phone uniqueness, we check if ANY user has this phone number
        // In MongoDB, we can use the repository to check
        var phoneExists = await _userRepository.PhoneNumberExistsAsync(user.PhoneNumber);

        if (phoneExists)
        {
            // For updates, we need to ensure it's not the same user
            // However, PhoneNumberExistsAsync currently doesn't check ID.
            // Let's refine the logic to find the user with this phone number.
            var existingUserWithPhone = await _userRepository.FindOneAsync(u => u.PhoneNumber == user.PhoneNumber);
            
            if (existingUserWithPhone != null && existingUserWithPhone.Id != user.Id)
            {
                _logger.LogWarning("Phone number already exists: {PhoneNumber}", user.PhoneNumber);
                return Result.Failure(
                    "Phone number already exists",
                    new List<string> { "A user with this phone number is already registered" },
                    "PHONE_ALREADY_EXISTS");
            }
        }

        return Result.Success();
    }
}
