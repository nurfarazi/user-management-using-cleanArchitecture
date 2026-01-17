using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using UserManagement.Shared.Contracts.Services;
using UserManagement.Shared.Models.Configurations;
using UserManagement.Shared.Models.Entities;

namespace UserManagement.Services.Implementations;

public class MailKitEmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<MailKitEmailService> _logger;

    public MailKitEmailService(IOptions<EmailSettings> emailSettings, ILogger<MailKitEmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task SendWelcomeEmailAsync(User user)
    {
        try
        {
            _logger.LogInformation("Sending welcome email to {Email}", user.Email);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            message.To.Add(new MailboxAddress($"{user.FirstName} {user.LastName}", user.Email));
            message.Subject = "Welcome to Our Platform!";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <h1>Welcome, {user.FirstName}!</h1>
                    <p>Thank you for registering on our platform. We are excited to have you on board!</p>
                    <p>Your registered email: {user.Email}</p>
                    <p>Best regards,<br/>The Team</p>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            // Connect to the SMTP server
            await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.Auto);

            // Authenticate if credentials are provided
            if (!string.IsNullOrEmpty(_emailSettings.SmtpUser) && !string.IsNullOrEmpty(_emailSettings.SmtpPass))
            {
                await client.AuthenticateAsync(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Welcome email sent successfully to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
            // We don't want to fail the registration if email sending fails, 
            // so we just log the error. In a production system, you might 
            // want to retry or use a background queue.
        }
    }
}
