using FluentValidation;
using UserManagement.Shared.Models.DTOs;

namespace UserManagement.API.Validators;

/// <summary>
/// Validator for RefreshTokenRequest DTOs.
/// Ensures refresh token is provided and has valid format.
/// </summary>
public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    /// <summary>
    /// Initializes a new instance of the RefreshTokenRequestValidator class.
    /// </summary>
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required")
            .MinimumLength(20)
            .WithMessage("Refresh token must be a valid token");
    }
}
