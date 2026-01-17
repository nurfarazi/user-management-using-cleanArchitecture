using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserManagement.Shared.Configuration;
using UserManagement.Shared.Contracts.Repositories;
using UserManagement.Shared.Contracts.Services;
using UserManagement.Shared.Contracts.Validators;
using UserManagement.Shared.Models.DTOs;
using UserManagement.Shared.Models.Entities;
using UserManagement.Shared.Models.Results;
using UserManagement.Shared.Utils;

namespace UserManagement.Services.Implementations;

/// <summary>
/// Service implementation for user management business logic.
/// Handles user registration, validation, and coordination with repository layer.
/// All password hashing is done here before repository access.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IEnumerable<IBusinessValidator<User>> _validators;
    private readonly IEmailService _emailService;
    private readonly ValidationSettings _settings;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository, 
        IEnumerable<IBusinessValidator<User>> validators,
        IEmailService emailService,
        IOptions<ValidationSettings> settings,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _validators = validators ?? throw new ArgumentNullException(nameof(validators));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a new user in the system.
    /// Performs the following steps:
    /// 1. Check if email already exists (business rule: email uniqueness)
    /// 2. Hash the password using BCrypt (security requirement)
    /// 3. Create User entity from DTO
    /// 4. Persist to database via repository
    /// 5. Return success response with user data
    /// </summary>
    /// <param name="request">The registration request containing user data.</param>
    /// <returns>Success result with user details, or failure result if business rules violated.</returns>
    public async Task<Result<RegisterUserResponse>> RegisterUserAsync(RegisterUserRequest request)
    {
        try
        {
            // Validate input
            if (request == null)
                return Result<RegisterUserResponse>.Failure(
                    "Registration request is required",
                    "INVALID_REQUEST");

            if (string.IsNullOrWhiteSpace(request.Email))
                return Result<RegisterUserResponse>.Failure(
                    "Email is required",
                    "MISSING_EMAIL");

            if (string.IsNullOrWhiteSpace(request.Password))
                return Result<RegisterUserResponse>.Failure(
                    "Password is required",
                    "MISSING_PASSWORD");

            _logger.LogInformation("User registration started for email: {Email}", request.Email);

            // Business Rule: Email normalization (RFC-aware, handles tags)
            var normalizedEmail = EmailNormalizer.Normalize(request.Email);

            // Create User domain entity (preliminary for validation)
            var user = new User
            {
                Email = normalizedEmail,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DisplayName = request.DisplayName,
                DateOfBirth = request.DateOfBirth,
                PhoneNumber = PhoneNormalizer.Normalize(request.PhoneNumber),
                Status = UserStatus.PendingVerification.ToStatusString(),
                TermsAcceptedVersion = request.TermsAcceptedVersion,
                PrivacyPolicyAcceptedVersion = request.PrivacyPolicyAcceptedVersion,
                RegistrationIp = request.RegistrationIp,
                RegistrationUserAgent = request.RegistrationUserAgent,
                IsDeleted = false,
                Version = 1,
                CreatedAt = DateTime.UtcNow
            };

            // Run business validators
            // This pattern allows us to easily add/remove business rules and reuse them for Updates
            foreach (var validator in _validators)
            {
                var validationResult = await validator.ValidateAsync(user);
                if (!validationResult.IsSuccess)
                {
                    _logger.LogWarning("Registration validation failed: {ErrorCode} - {ErrorMessage}", 
                        validationResult.ErrorCode, validationResult.ErrorMessage);

                    return Result<RegisterUserResponse>.Failure(
                        validationResult.ErrorMessage ?? "Business rule violation",
                        validationResult.Errors,
                        validationResult.ErrorCode);
                }
            }

            // Hash password using BCrypt
            _logger.LogInformation("Hashing password for user: {Email}", normalizedEmail);
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);
            user.PasswordHash = passwordHash;
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            // Persist to database
            _logger.LogInformation("Persisting user to database: {Email}", normalizedEmail);
            var createdUser = await _userRepository.AddAsync(user);

            // Map to response DTO
            _logger.LogInformation("User registration completed successfully for email: {Email}", normalizedEmail);
            var response = new RegisterUserResponse
            {
                UserId = createdUser.Id,
                Email = createdUser.Email,
                FirstName = createdUser.FirstName,
                LastName = createdUser.LastName,
                DisplayName = createdUser.DisplayName,
                DateOfBirth = createdUser.DateOfBirth,
                PhoneNumber = createdUser.PhoneNumber,
                CreatedAt = createdUser.CreatedAt
            };

            // Send welcome email (asynchronously, don't wait if not necessary but we are in async method)
            // We don't await this if we want it to be fire-and-forget, but since it's a small task, 
            // and we want to ensure it's logged correctly, we can await it or use Task.Run.
            // For now, let's await it to keep it simple, given the implementation handles exceptions.
            await _emailService.SendWelcomeEmailAsync(createdUser);

            return Result<RegisterUserResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during user registration for email: {Email}", request?.Email);
            return Result<RegisterUserResponse>.Failure(
                ex,
                "REGISTRATION_ERROR");
        }
    }

    /// <summary>
    /// Updates an existing user's profile information.
    /// Performs the following steps:
    /// 1. Verify user exists by ID
    /// 2. Apply updates while preserving immutable fields (Email, Password, IsDeleted)
    /// 3. Run business validators (same as RegisterUser)
    /// 4. Persist changes to database
    /// </summary>
    /// <param name="request">The update request containing new user data.</param>
    /// <returns>Success result with updated user details, or failure result.</returns>
    public async Task<Result<UpdateUserResponse>> UpdateUserAsync(UpdateUserRequest request)
    {
        try
        {
            if (request == null)
                return Result<UpdateUserResponse>.Failure("Update request is required", "INVALID_REQUEST");

            if (string.IsNullOrWhiteSpace(request.Id))
                return Result<UpdateUserResponse>.Failure("User ID is required", "MISSING_USER_ID");

            _logger.LogInformation("Updating user with ID: {UserId}", request.Id);

            // 1. Verify user exists
            var existingUser = await _userRepository.GetByIdAsync(request.Id);
            if (existingUser == null)
            {
                _logger.LogWarning("Update failed: User with ID {UserId} not found", request.Id);
                return Result<UpdateUserResponse>.Failure(
                    $"User with ID {request.Id} not found", 
                    "USER_NOT_FOUND");
            }

            // 1.5 Optimistic Concurrency Check
            if (existingUser.Version != request.Version)
            {
                _logger.LogWarning("Update conflict for user {UserId}: Expected version {Expected}, got {Actual}", 
                    request.Id, existingUser.Version, request.Version);
                return Result<UpdateUserResponse>.Failure(
                    "The profile has been updated by another process. Please refresh and try again.", 
                    "CONCURRENCY_CONFLICT");
            }

            // 2. Prepare domain entity for validation and update
            // We preserve Immutable Fields: Email, PasswordHash, IsDeleted, CreatedAt
            var userToUpdate = new User
            {
                Id = existingUser.Id,
                Email = existingUser.Email, // Cannot be updated
                PasswordHash = existingUser.PasswordHash, // Cannot be updated via this flow
                PasswordHistory = existingUser.PasswordHistory,
                IsDeleted = existingUser.IsDeleted, // Cannot be updated via this flow
                CreatedAt = existingUser.CreatedAt,
                Status = existingUser.Status,
                Role = existingUser.Role,
                TermsAcceptedVersion = existingUser.TermsAcceptedVersion,
                PrivacyPolicyAcceptedVersion = existingUser.PrivacyPolicyAcceptedVersion,
                
                // Fields that CAN be updated
                FirstName = request.FirstName,
                LastName = request.LastName,
                DisplayName = request.DisplayName,
                DateOfBirth = request.DateOfBirth,
                PhoneNumber = PhoneNormalizer.Normalize(request.PhoneNumber),
                
                // Increment version
                Version = existingUser.Version + 1,
                UpdatedAt = DateTime.UtcNow
            };

            // 3. Run business validators
            foreach (var validator in _validators)
            {
                var validationResult = await validator.ValidateAsync(userToUpdate);
                if (!validationResult.IsSuccess)
                {
                    _logger.LogWarning("Update validation failed for user {UserId}: {ErrorCode} - {ErrorMessage}", 
                        request.Id, validationResult.ErrorCode, validationResult.ErrorMessage);

                    return Result<UpdateUserResponse>.Failure(
                        validationResult.ErrorMessage ?? "Business rule violation",
                        validationResult.Errors,
                        validationResult.ErrorCode);
                }
            }

            // 4. Persist to database
            var success = await _userRepository.UpdateAsync(request.Id, userToUpdate);
            if (!success)
            {
                _logger.LogError("Database update failed for user: {UserId}", request.Id);
                return Result<UpdateUserResponse>.Failure(
                    "Failed to update user in database",
                    "DATABASE_UPDATE_ERROR");
            }

            // Map to response DTO
            var response = new UpdateUserResponse
            {
                UserId = userToUpdate.Id!,
                Email = userToUpdate.Email,
                FirstName = userToUpdate.FirstName,
                LastName = userToUpdate.LastName,
                DisplayName = userToUpdate.DisplayName,
                DateOfBirth = userToUpdate.DateOfBirth,
                PhoneNumber = userToUpdate.PhoneNumber,
                UpdatedAt = userToUpdate.UpdatedAt,
                Version = userToUpdate.Version
            };

            _logger.LogInformation("User updated successfully: {UserId}", request.Id);
            return Result<UpdateUserResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during user update for ID: {UserId}", request?.Id);
            return Result<UpdateUserResponse>.Failure(ex, "UPDATE_ERROR");
        }
    }

    /// <summary>
    /// Retrieves a single user by ID.
    /// </summary>
    /// <param name="userId">The ID of the user to retrieve.</param>
    /// <returns>Result containing user data as UserDto, or failure if not found.</returns>
    public async Task<Result<UserDto>> GetUserByIdAsync(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Result<UserDto>.Failure("User ID is required", "MISSING_USER_ID");

            _logger.LogInformation("Retrieving user by ID: {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found with ID: {UserId}", userId);
                return Result<UserDto>.Failure($"User with ID {userId} not found", "USER_NOT_FOUND");
            }

            // Exclude soft-deleted users
            if (user.IsDeleted)
            {
                _logger.LogWarning("User is deleted: {UserId}", userId);
                return Result<UserDto>.Failure("User not found", "USER_NOT_FOUND");
            }

            var userDto = MapUserToDto(user);
            _logger.LogInformation("User retrieved successfully: {UserId}", userId);
            return Result<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving user by ID: {UserId}", userId);
            return Result<UserDto>.Failure(ex, "RETRIEVAL_ERROR");
        }
    }

    /// <summary>
    /// Retrieves a paginated list of users with filtering and sorting.
    /// </summary>
    /// <param name="request">Pagination, filtering, and sorting parameters.</param>
    /// <returns>Result containing paged user data, or failure if error occurs.</returns>
    public async Task<Result<PagedResult<UserDto>>> GetUsersAsync(GetUsersRequest request)
    {
        try
        {
            if (request == null)
                return Result<PagedResult<UserDto>>.Failure("Request is required", "INVALID_REQUEST");

            _logger.LogInformation("Retrieving paged users: Page {PageNumber}, Size {PageSize}",
                request.PageNumber, request.PageSize);

            // Get paged users from repository
            var pagedUsers = await _userRepository.GetPagedAsync(request);

            // Map to UserDto
            var userDtos = pagedUsers.Items.Select(MapUserToDto).ToList();

            // Create paged result of DTOs
            var pagedResult = PagedResult<UserDto>.Create(
                userDtos,
                pagedUsers.TotalCount,
                pagedUsers.PageNumber,
                pagedUsers.PageSize);

            _logger.LogInformation("Retrieved {Count} users out of {Total}",
                userDtos.Count, pagedUsers.TotalCount);

            return Result<PagedResult<UserDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving paged users");
            return Result<PagedResult<UserDto>>.Failure(ex, "RETRIEVAL_ERROR");
        }
    }

    /// <summary>
    /// Soft deletes a user (marks as deleted without physical removal).
    /// </summary>
    /// <param name="userId">The ID of the user to delete.</param>
    /// <returns>Result indicating success or failure.</returns>
    public async Task<Result> SoftDeleteUserAsync(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Result.Failure("User ID is required", "MISSING_USER_ID");

            _logger.LogInformation("Soft deleting user: {UserId}", userId);

            // Verify user exists
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for deletion: {UserId}", userId);
                return Result.Failure($"User with ID {userId} not found", "USER_NOT_FOUND");
            }

            // Soft delete
            var success = await _userRepository.SoftDeleteAsync(userId);
            if (success)
            {
                _logger.LogInformation("User soft deleted successfully: {UserId}", userId);
                return Result.Success();
            }
            else
            {
                _logger.LogError("Failed to soft delete user: {UserId}", userId);
                return Result.Failure("Failed to delete user", "DELETION_FAILED");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error soft deleting user: {UserId}", userId);
            return Result.Failure(ex, "DELETION_ERROR");
        }
    }

    /// <summary>
    /// Changes a user's password.
    /// </summary>
    public async Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        try
        {
            if (request == null) return Result.Failure("Request is required", "INVALID_REQUEST");
            
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || user.IsDeleted) return Result.Failure("User not found", "USER_NOT_FOUND");

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                _logger.LogWarning("Password change failed: Incorrect current password for user {UserId}", userId);
                return Result.Failure("Incorrect current password", "INVALID_PASSWORD");
            }

            // Enforce password history (Must not match last N passwords)
            foreach (var oldHash in user.PasswordHistory)
            {
                if (BCrypt.Net.BCrypt.Verify(request.NewPassword, oldHash))
                {
                    return Result.Failure("New password cannot be one of your previous passwords", "PASSWORD_USED_BEFORE");
                }
            }

            // Update password
            var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
            
            // Add current hash to history
            user.PasswordHistory.Add(user.PasswordHash);
            if (user.PasswordHistory.Count > _settings.PasswordHistoryLimit)
            {
                user.PasswordHistory.RemoveAt(0);
            }

            user.PasswordHash = newHash;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(userId, user);
            
            _logger.LogInformation("Password changed successfully for user {UserId}", userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return Result.Failure(ex, "PASSWORD_CHANGE_ERROR");
        }
    }

    /// <summary>
    /// Updates user status.
    /// </summary>
    public async Task<Result> UpdateUserStatusAsync(string userId, UpdateUserStatusRequest request)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return Result.Failure("User not found", "USER_NOT_FOUND");

            _logger.LogInformation("Updating status for user {UserId} to {NewStatus}. Reason: {ReasonCode}", 
                userId, request.NewStatus, request.ReasonCode);

            user.Status = request.NewStatus;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(userId, user);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for user {UserId}", userId);
            return Result.Failure(ex, "STATUS_UPDATE_ERROR");
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
