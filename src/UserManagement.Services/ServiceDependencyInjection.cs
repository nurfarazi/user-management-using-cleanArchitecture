using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Services.Implementations;
using UserManagement.Services.Validators;
using UserManagement.Shared.Models.Configurations;
using UserManagement.Shared.Configuration;
using UserManagement.Shared.Contracts.Services;
using UserManagement.Shared.Contracts.Validators;
using UserManagement.Shared.Models.Entities;

namespace UserManagement.Services;

/// <summary>
/// Dependency injection extension methods for the Service layer.
/// Registers all service implementations.
/// </summary>
public static class ServiceDependencyInjection
{
    /// <summary>
    /// Adds all service layer implementations to the DI container.
    /// Services are registered as scoped per HTTP request.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <param name="configuration">The configuration to bind settings from.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddServiceLayer(this IServiceCollection services, IConfiguration configuration)
    {
        // Register all services as scoped
        // Scoped means a new instance per HTTP request in web context
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IEmailService, MailKitEmailService>();

        // Configure Settings
        services.Configure<EmailSettings>(configuration.GetSection(nameof(EmailSettings)));
        services.Configure<ValidationSettings>(configuration.GetSection(nameof(ValidationSettings)));

        // Register Business Validators
        services.AddScoped<IBusinessValidator<User>, EmailUniquenessValidator>();
        services.AddScoped<IBusinessValidator<User>, PhoneUniquenessValidator>();
        services.AddScoped<IBusinessValidator<User>, PasswordHistoryValidator>();

        return services;
    }
}
