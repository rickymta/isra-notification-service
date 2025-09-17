using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Settings;
using NotificationService.Domain.Entities;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Text.RegularExpressions;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// SendGrid implementation of email service
/// </summary>
public class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _sendGridClient;
    private readonly EmailSettings _settings;
    private readonly ILogger<SendGridEmailService> _logger;
    private static readonly Regex EmailRegex = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

    public SendGridEmailService(
        IOptions<EmailSettings> settings,
        ILogger<SendGridEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        
        if (string.IsNullOrEmpty(_settings.SendGridApiKey))
        {
            throw new InvalidOperationException("SendGrid API key is not configured");
        }

        _sendGridClient = new SendGridClient(_settings.SendGridApiKey);
    }

    public async Task<NotificationResult> SendEmailAsync(NotificationContent content, NotificationRecipient recipient, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ValidateRecipient(recipient))
            {
                return NotificationResult.Failure("Invalid email recipient");
            }

            var from = new EmailAddress(_settings.FromEmail, _settings.FromName);
            var to = new EmailAddress(recipient.Email!, GetRecipientName(recipient));
            
            var msg = MailHelper.CreateSingleEmail(
                from,
                to,
                content.Subject ?? "Notification",
                plainTextContent: ExtractPlainText(content.Body),
                htmlContent: content.Body);

            // Add custom headers
            msg.AddCustomArg("user_id", recipient.UserId ?? "unknown");
            msg.AddCustomArg("language", recipient.Language);
            
            if (recipient.TimeZone != null)
            {
                msg.AddCustomArg("timezone", recipient.TimeZone);
            }

            // Add variables as custom args for tracking
            foreach (var variable in content.Variables)
            {
                msg.AddCustomArg($"var_{variable.Key}", variable.Value);
            }

            _logger.LogInformation("Sending email to {Email} with subject {Subject}", 
                recipient.Email, content.Subject);

            var response = await _sendGridClient.SendEmailAsync(msg, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var messageId = response.Headers?.GetValues("X-Message-Id")?.FirstOrDefault();
                
                _logger.LogInformation("Email sent successfully to {Email}, MessageId: {MessageId}", 
                    recipient.Email, messageId);

                return NotificationResult.Success(messageId);
            }
            else
            {
                var errorBody = await response.Body.ReadAsStringAsync();
                _logger.LogError("Failed to send email to {Email}. Status: {StatusCode}, Body: {ErrorBody}",
                    recipient.Email, response.StatusCode, errorBody);

                return NotificationResult.Failure($"SendGrid API error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending email to {Email}", recipient.Email);
            return NotificationResult.Failure($"Exception: {ex.Message}");
        }
    }

    public bool ValidateRecipient(NotificationRecipient recipient)
    {
        if (string.IsNullOrEmpty(recipient.Email))
        {
            _logger.LogWarning("Email recipient validation failed: Email is null or empty");
            return false;
        }

        if (!EmailRegex.IsMatch(recipient.Email))
        {
            _logger.LogWarning("Email recipient validation failed: Invalid email format {Email}", recipient.Email);
            return false;
        }

        return true;
    }

    private static string GetRecipientName(NotificationRecipient recipient)
    {
        // Try to extract name from variables or use user ID
        return recipient.UserId ?? "User";
    }

    private static string ExtractPlainText(string htmlContent)
    {
        if (string.IsNullOrEmpty(htmlContent))
            return string.Empty;

        // Simple HTML tag removal for plain text version
        var plainText = Regex.Replace(htmlContent, "<[^>]*>", string.Empty);
        return System.Net.WebUtility.HtmlDecode(plainText);
    }
}