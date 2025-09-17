using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Settings;
using NotificationService.Domain.Entities;
using System.Text.RegularExpressions;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Twilio implementation of SMS service
/// </summary>
public class TwilioSmsService : ISmsService
{
    private readonly SmsSettings _settings;
    private readonly ILogger<TwilioSmsService> _logger;
    private static readonly Regex PhoneRegex = new(@"^\+[1-9]\d{1,14}$", RegexOptions.Compiled);

    public TwilioSmsService(
        IOptions<SmsSettings> settings,
        ILogger<TwilioSmsService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        
        if (string.IsNullOrEmpty(_settings.TwilioAccountSid) || 
            string.IsNullOrEmpty(_settings.TwilioAuthToken) ||
            string.IsNullOrEmpty(_settings.TwilioFromNumber))
        {
            throw new InvalidOperationException("Twilio credentials are not properly configured");
        }

        TwilioClient.Init(_settings.TwilioAccountSid, _settings.TwilioAuthToken);
    }

    public async Task<NotificationResult> SendSmsAsync(NotificationContent content, NotificationRecipient recipient, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ValidateRecipient(recipient))
            {
                return NotificationResult.Failure("Invalid SMS recipient");
            }

            var fromPhoneNumber = new PhoneNumber(_settings.TwilioFromNumber);
            var toPhoneNumber = new PhoneNumber(recipient.PhoneNumber!);

            _logger.LogInformation("Sending SMS to {PhoneNumber}", recipient.PhoneNumber);

            var message = await MessageResource.CreateAsync(
                body: content.Body,
                from: fromPhoneNumber,
                to: toPhoneNumber,
                pathAccountSid: _settings.TwilioAccountSid);

            if (message.ErrorCode == null)
            {
                _logger.LogInformation("SMS sent successfully to {PhoneNumber}, MessageSid: {MessageSid}", 
                    recipient.PhoneNumber, message.Sid);

                var result = NotificationResult.Success(message.Sid);
                result.Metadata["status"] = message.Status?.ToString() ?? "unknown";
                result.Metadata["price"] = message.Price?.ToString() ?? "0";
                result.Metadata["price_unit"] = message.PriceUnit ?? "USD";
                
                return result;
            }
            else
            {
                _logger.LogError("Failed to send SMS to {PhoneNumber}. ErrorCode: {ErrorCode}, ErrorMessage: {ErrorMessage}",
                    recipient.PhoneNumber, message.ErrorCode, message.ErrorMessage);

                return NotificationResult.Failure($"Twilio error {message.ErrorCode}: {message.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending SMS to {PhoneNumber}", recipient.PhoneNumber);
            return NotificationResult.Failure($"Exception: {ex.Message}");
        }
    }

    public bool ValidateRecipient(NotificationRecipient recipient)
    {
        if (string.IsNullOrEmpty(recipient.PhoneNumber))
        {
            _logger.LogWarning("SMS recipient validation failed: Phone number is null or empty");
            return false;
        }

        if (!PhoneRegex.IsMatch(recipient.PhoneNumber))
        {
            _logger.LogWarning("SMS recipient validation failed: Invalid phone number format {PhoneNumber}", recipient.PhoneNumber);
            return false;
        }

        return true;
    }
}