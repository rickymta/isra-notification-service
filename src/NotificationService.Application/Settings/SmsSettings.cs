namespace NotificationService.Application.Settings;

/// <summary>
/// Configuration settings for SMS service
/// </summary>
public class SmsSettings
{
    public const string SectionName = "Sms";

    /// <summary>
    /// SMS provider (Twilio, etc.)
    /// </summary>
    public string Provider { get; set; } = "Twilio";

    /// <summary>
    /// Twilio Account SID
    /// </summary>
    public string? TwilioAccountSid { get; set; }

    /// <summary>
    /// Twilio Auth Token
    /// </summary>
    public string? TwilioAuthToken { get; set; }

    /// <summary>
    /// Twilio phone number to send from
    /// </summary>
    public string? TwilioFromNumber { get; set; }
}