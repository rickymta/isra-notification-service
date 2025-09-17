namespace NotificationService.Application.Settings;

/// <summary>
/// Configuration settings for email service
/// </summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>
    /// Email provider (SendGrid, SMTP, etc.)
    /// </summary>
    public string Provider { get; set; } = "SendGrid";

    /// <summary>
    /// SendGrid API key
    /// </summary>
    public string? SendGridApiKey { get; set; }

    /// <summary>
    /// Default sender email address
    /// </summary>
    public string FromEmail { get; set; } = "noreply@example.com";

    /// <summary>
    /// Default sender name
    /// </summary>
    public string FromName { get; set; } = "Notification Service";

    /// <summary>
    /// SMTP settings (if using SMTP provider)
    /// </summary>
    public SmtpSettings Smtp { get; set; } = new();
}

/// <summary>
/// SMTP configuration settings
/// </summary>
public class SmtpSettings
{
    /// <summary>
    /// SMTP server host
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Username for SMTP authentication
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for SMTP authentication
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Enable SSL/TLS
    /// </summary>
    public bool EnableSsl { get; set; } = true;
}