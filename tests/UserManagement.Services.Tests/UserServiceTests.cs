using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using UserManagement.Shared.Configuration;
using UserManagement.Services.Implementations;
using UserManagement.Shared.Contracts.Repositories;
using UserManagement.Shared.Contracts.Services;
using UserManagement.Shared.Contracts.Validators;
using UserManagement.Shared.Models.DTOs;
using UserManagement.Shared.Models.Entities;
using UserManagement.Shared.Models.Results;
using Xunit;

namespace UserManagement.Services.Tests;

/// <summary>
/// Unit tests for UserService.RegisterUserAsync method.
/// Tests service layer business logic with mocked repository dependencies.
/// Demonstrates the unit testability of the service layer architecture.
/// </summary>
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IBusinessValidator<User>> _mockEmailValidator;
    private readonly Mock<IBusinessValidator<User>> _mockPhoneValidator;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _userService;

    /// <summary>
    /// Initializes a new instance of UserServiceTests.
    /// Sets up mocks for each test.
    /// </summary>
    public UserServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockEmailValidator = new Mock<IBusinessValidator<User>>();
        _mockPhoneValidator = new Mock<IBusinessValidator<User>>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<UserService>>();
        var settings = Options.Create(new ValidationSettings());

        var validators = new List<IBusinessValidator<User>>
        {
            _mockEmailValidator.Object,
            _mockPhoneValidator.Object
        };

        _userService = new UserService(
            _mockUserRepository.Object, 
            validators, 
            _mockEmailService.Object, 
            settings,
            _mockLogger.Object);

        // Default setup: validators return success
        _mockEmailValidator
            .Setup(v => v.ValidateAsync(It.IsAny<User>()))
            .ReturnsAsync(Result.Success());
        _mockPhoneValidator
            .Setup(v => v.ValidateAsync(It.IsAny<User>()))
            .ReturnsAsync(Result.Success());
    }

    /// <summary>
    /// Test: RegisterUserAsync with valid request returns success.
    /// Verifies that a new user is created and returned successfully.
    /// </summary>
    [Fact]
    public async Task RegisterUserAsync_WithValidRequest_ReturnsSuccessResult()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Email = "john.doe@example.com",
            Password = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "+1234567890"
        };

        var expectedUser = new User
        {
            Id = "507f1f77bcf86cd799439011",
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            CreatedAt = DateTime.UtcNow
        };

        // Mock: AddAsync returns the user with ID

        // Mock: AddAsync returns the user with ID
        _mockUserRepository
            .Setup(repo => repo.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) =>
            {
                user.Id = expectedUser.Id;
                return user;
            });

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(request.Email);
        result.Value.FirstName.Should().Be(request.FirstName);
        result.Value.LastName.Should().Be(request.LastName);
        result.Value.UserId.Should().NotBeNullOrEmpty();

        // Verify validators were called
        _mockEmailValidator.Verify(
            v => v.ValidateAsync(It.IsAny<User>()),
            Times.Once);

        _mockPhoneValidator.Verify(
            v => v.ValidateAsync(It.IsAny<User>()),
            Times.Once);

        _mockUserRepository.Verify(
            repo => repo.AddAsync(It.IsAny<User>()),
            Times.Once,
            "Should add user to repository");

        _mockEmailService.Verify(
            s => s.SendWelcomeEmailAsync(It.IsAny<User>()),
            Times.Once,
            "Should send welcome email");
    }

    /// <summary>
    /// Test: RegisterUserAsync with existing email returns failure.
    /// Verifies that duplicate emails are rejected with appropriate error.
    /// </summary>
    [Fact]
    public async Task RegisterUserAsync_WithExistingEmail_ReturnsFailureResult()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Email = "existing@example.com",
            Password = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Mock: Email validator returns failure
        _mockEmailValidator
            .Setup(v => v.ValidateAsync(It.IsAny<User>()))
            .ReturnsAsync(Result.Failure(
                "Email already exists",
                new List<string> { "A user with this email address is already registered" },
                "EMAIL_ALREADY_EXISTS"));

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("EMAIL_ALREADY_EXISTS");
        result.ErrorMessage.Should().Contain("Email already exists");
        result.Errors.Should().Contain(e => e.Contains("already registered"));

        // Verify repository AddAsync was NOT called
        _mockUserRepository.Verify(
            repo => repo.AddAsync(It.IsAny<User>()),
            Times.Never,
            "Should not add user when email already exists");

        _mockEmailService.Verify(
            s => s.SendWelcomeEmailAsync(It.IsAny<User>()),
            Times.Never,
            "Should not send email when validation fails");
    }

    /// <summary>
    /// Test: RegisterUserAsync validates repository method calls.
    /// Verifies that repository methods are called with correct parameters.
    /// </summary>
    [Fact]
    public async Task RegisterUserAsync_ValidRequest_CallsRepositoryMethodsCorrectly()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Email = "test@example.com",
            Password = "SecurePass123!",
            FirstName = "Test",
            LastName = "User"
        };

        _mockUserRepository
            .Setup(repo => repo.EmailExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _mockUserRepository
            .Setup(repo => repo.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) =>
            {
                user.Id = "507f1f77bcf86cd799439011";
                return user;
            });

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify validators were called
        _mockEmailValidator.Verify(
            v => v.ValidateAsync(It.IsAny<User>()),
            Times.Once);

        // Verify AddAsync was called with User entity containing correct data
        _mockUserRepository.Verify(
            repo => repo.AddAsync(It.Is<User>(u =>
                u.Email == request.Email &&
                u.FirstName == request.FirstName &&
                u.LastName == request.LastName
            )),
            Times.Once);
    }

    /// <summary>
    /// Test: RegisterUserAsync with null request returns failure.
    /// Verifies that null requests are handled gracefully.
    /// </summary>
    [Fact]
    public async Task RegisterUserAsync_WithNullRequest_ReturnsFailureResult()
    {
        // Act
        var result = await _userService.RegisterUserAsync(null!);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("request is required");

        // Verify validators were not called
        _mockEmailValidator.Verify(
            v => v.ValidateAsync(It.IsAny<User>()),
            Times.Never);
    }

    /// <summary>
    /// Test: RegisterUserAsync with empty email returns failure.
    /// Verifies that empty emails are rejected.
    /// </summary>
    [Fact]
    public async Task RegisterUserAsync_WithEmptyEmail_ReturnsFailureResult()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Email = string.Empty,
            Password = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("MISSING_EMAIL");
    }

    /// <summary>
    /// Test: RegisterUserAsync with empty password returns failure.
    /// Verifies that empty passwords are rejected.
    /// </summary>
    [Fact]
    public async Task RegisterUserAsync_WithEmptyPassword_ReturnsFailureResult()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Email = "test@example.com",
            Password = string.Empty,
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("MISSING_PASSWORD");
    }

    /// <summary>
    /// Test: RegisterUserAsync hashes password before persistence.
    /// Verifies that passwords are hashed using BCrypt.
    /// </summary>
    [Fact]
    public async Task RegisterUserAsync_ValidRequest_PasswordIsHashed()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Email = "test@example.com",
            Password = "SecurePass123!",
            FirstName = "Test",
            LastName = "User"
        };

        User? capturedUser = null;

        _mockUserRepository
            .Setup(repo => repo.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .ReturnsAsync((User user) =>
            {
                user.Id = "507f1f77bcf86cd799439011";
                return user;
            });

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedUser.Should().NotBeNull();
        capturedUser!.PasswordHash.Should().NotBe(request.Password);
        capturedUser.PasswordHash.Should().StartWith("$2"); // BCrypt hash format
        BCrypt.Net.BCrypt.Verify(request.Password, capturedUser.PasswordHash).Should().BeTrue();
    }

    /// <summary>
    /// Test: RegisterUserAsync handles repository exceptions.
    /// Verifies that repository errors result in failure results.
    /// </summary>
    [Fact]
    public async Task RegisterUserAsync_RepositoryThrowsException_ReturnsFailureResult()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Email = "test@example.com",
            Password = "SecurePass123!",
            FirstName = "Test",
            LastName = "User"
        };

        _mockEmailValidator
            .Setup(v => v.ValidateAsync(It.IsAny<User>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("REGISTRATION_ERROR");
    }

    /// <summary>
    /// Test: UpdateUserAsync with valid request returns success.
    /// Verifies that user is retrieved, validated, and updated.
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_WithValidRequest_ReturnsSuccessResult()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";
        var request = new UpdateUserRequest
        {
            Id = userId,
            FirstName = "UpdatedName",
            LastName = "UpdatedLast",
            PhoneNumber = "+8801711223344"
        };

        var existingUser = new User
        {
            Id = userId,
            Email = "original@example.com",
            PasswordHash = "OriginalHash",
            FirstName = "Original",
            LastName = "User",
            IsDeleted = false
        };

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);

        _mockUserRepository
            .Setup(repo => repo.UpdateAsync(userId, It.IsAny<User>()))
            .ReturnsAsync(true);

        // Act
        var result = await _userService.UpdateUserAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.FirstName.Should().Be(request.FirstName);
        result.Value.Email.Should().Be(existingUser.Email); // Should be preserved

        _mockUserRepository.Verify(repo => repo.UpdateAsync(userId, It.Is<User>(u => 
            u.FirstName == request.FirstName &&
            u.Email == existingUser.Email &&
            u.PasswordHash == existingUser.PasswordHash &&
            u.IsDeleted == existingUser.IsDeleted
        )), Times.Once);
    }

    /// <summary>
    /// Test: UpdateUserAsync with non-existent user returns failure.
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_UserNotFound_ReturnsFailureResult()
    {
        // Arrange
        var userId = "nonexistent";
        var request = new UpdateUserRequest { Id = userId };

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.UpdateUserAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("USER_NOT_FOUND");
    }

    /// <summary>
    /// Test: UpdateUserAsync validation failure returns error.
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_ValidationFails_ReturnsFailureResult()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";
        var request = new UpdateUserRequest { Id = userId, PhoneNumber = "+880invalid" };

        var existingUser = new User { Id = userId, Email = "test@example.com" };

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);

        _mockPhoneValidator
            .Setup(v => v.ValidateAsync(It.IsAny<User>()))
            .ReturnsAsync(Result.Failure("Invalid phone", "PHONE_ALREADY_EXISTS"));

        // Act
        var result = await _userService.UpdateUserAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PHONE_ALREADY_EXISTS");
        
        _mockUserRepository.Verify(repo => repo.UpdateAsync(userId, It.IsAny<User>()), Times.Never);
    }
}
