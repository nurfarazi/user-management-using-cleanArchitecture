using UserManagement.Shared.Models.Entities;

namespace UserManagement.Shared.Contracts.Services;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(User user);
}
