using UserManagement.Shared.Models.Results;

namespace UserManagement.Shared.Contracts.Validators;

/// <summary>
/// Generic interface for business rule validation.
/// Logic defined here can be reused across different service operations (e.g., Register and Update).
/// </summary>
/// <typeparam name="T">The type of the entity or model being validated.</typeparam>
public interface IBusinessValidator<T>
{
    /// <summary>
    /// Validates the provided subject against specific business rules.
    /// </summary>
    /// <param name="subject">The entity or model to validate.</param>
    /// <returns>A Result indicating success or the specific business rule violation.</returns>
    Task<Result> ValidateAsync(T subject);
}
