using FluentValidation;
using UserManagement.Shared.Models.DTOs;

namespace UserManagement.API.Validators;

/// <summary>
/// FluentValidation validator for UpdateUserRequest.
/// Reuses validation logic from registration where applicable.
/// </summary>
public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("User ID is required");

        ConfigureNameValidation();
        ConfigurePhoneValidation();
        ConfigureDateOfBirthValidation();
    }

    /// <summary>
    /// Configures first and last name validation rules.
    /// Same rules as RegisterUser.
    /// </summary>
    private void ConfigureNameValidation()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required")
            .Length(2, 50)
            .WithMessage("First name must be between 2 and 50 characters")
            .Matches(@"^[a-zA-Z\s'-]+$")
            .WithMessage("First name can only contain letters, spaces, hyphens, and apostrophes");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required")
            .Length(2, 50)
            .WithMessage("Last name must be between 2 and 50 characters")
            .Matches(@"^[a-zA-Z\s'-]+$")
            .WithMessage("Last name can only contain letters, spaces, hyphens, and apostrophes");
    }

    /// <summary>
    /// Configures phone number validation rules.
    /// Same rules as RegisterUser.
    /// </summary>
    private void ConfigurePhoneValidation()
    {
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[0-9]{7,15}$")
            .WithMessage("Phone number must be between 7 and 15 digits and can optionally start with '+'")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }

    /// <summary>
    /// Configures date of birth validation rules.
    /// Same rules as RegisterUser.
    /// </summary>
    private void ConfigureDateOfBirthValidation()
    {
        RuleFor(x => x.DateOfBirth)
            .Must(dob => dob == null || dob.Value <= DateTime.Today.AddYears(-13))
            .WithMessage("User must be at least 13 years old")
            .When(x => x.DateOfBirth.HasValue);
    }
}
