using FluentValidation;
using UserManagement.Shared.Models.DTOs;

namespace UserManagement.API.Validators;

/// <summary>
/// Validator for GetUsersRequest DTOs.
/// Ensures pagination parameters are valid and sort criteria are recognized.
/// </summary>
public class GetUsersRequestValidator : AbstractValidator<GetUsersRequest>
{
    private static readonly List<string> ValidSortFields = new()
    {
        "email", "firstname", "lastname", "createdat", "role"
    };

    private static readonly List<string> ValidSortOrders = new()
    {
        "asc", "desc"
    };

    /// <summary>
    /// Initializes a new instance of the GetUsersRequestValidator class.
    /// </summary>
    public GetUsersRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100)
            .WithMessage("Page size cannot exceed 100 items");

        RuleFor(x => x.SortBy)
            .Must(x => string.IsNullOrEmpty(x) || ValidSortFields.Contains(x.ToLowerInvariant()))
            .WithMessage($"SortBy must be one of: {string.Join(", ", ValidSortFields)}");

        RuleFor(x => x.SortOrder)
            .Must(x => string.IsNullOrEmpty(x) || ValidSortOrders.Contains(x.ToLowerInvariant()))
            .WithMessage($"SortOrder must be one of: {string.Join(", ", ValidSortOrders)}");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(100)
            .WithMessage("Search term cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.SearchTerm));

        RuleFor(x => x.Role)
            .Must(x => string.IsNullOrEmpty(x) || x.Equals("User", StringComparison.OrdinalIgnoreCase) || x.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Role must be either 'User' or 'Admin'")
            .When(x => !string.IsNullOrEmpty(x.Role));

        RuleFor(x => x.CreatedBefore)
            .GreaterThan(x => x.CreatedAfter)
            .WithMessage("CreatedBefore must be after CreatedAfter")
            .When(x => x.CreatedAfter.HasValue && x.CreatedBefore.HasValue);
    }
}
