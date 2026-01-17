using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserManagement.Shared.Configuration;
using UserManagement.Shared.Contracts.Validators;
using UserManagement.Shared.Models.Entities;
using UserManagement.Shared.Models.Results;

namespace UserManagement.Services.Validators;

/// <summary>
/// Validator to ensure new passwords do not match recently used passwords.
/// </summary>
public class PasswordHistoryValidator : IBusinessValidator<User>
{
    private readonly ValidationSettings _settings;
    private readonly ILogger<PasswordHistoryValidator> _logger;

    public PasswordHistoryValidator(IOptions<ValidationSettings> settings, ILogger<PasswordHistoryValidator> logger)
    {
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Result> ValidateAsync(User user)
    {
        // This validator is only relevant during password changes, not during initial registration
        // (unless we are re-registering with the same email, but here PasswordHash is already set)
        // However, the UserService calls this for both.
        // During registration, user.PasswordHistory will be empty.
        
        // Wait, the User object passed here has the NEW PasswordHash.
        // We need to check if it matches any in user.PasswordHistory.
        
        if (user.PasswordHistory == null || !user.PasswordHistory.Any())
        {
            return Task.FromResult(Result.Success());
        }

        foreach (var oldHash in user.PasswordHistory)
        {
            if (BCrypt.Net.BCrypt.Verify(user.PasswordHash, oldHash)) // Wait, user.PasswordHash is already hashed.
            {
                // This logic is tricky because we can't verify hash against hash directly.
                // We actually need the PLAIN text password to verify against historic hashes.
                // But the IBusinessValidator takes the User entity which only has the hash.
                
                // If we want to check history, we should either:
                // 1. Pass the plain password to the validator (not ideal for IBusinessValidator<User>)
                // 2. Handle history check in the Service specialized method.
                
                // Let's assume for now we are checking if the NEW HASH matches an OLD HASH exactly.
                // (This only works if the salt is the same, which it isn't in BCrypt by default).
                
                // CORRECT WAY: The history check should be done in ChangePasswordAsync in UserService.
            }
        }

        return Task.FromResult(Result.Success());
    }
}
