using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Entities;

/// <summary>
/// Represents the history of a notification that was sent
/// </summary>
public class NotificationHistory : BaseEntity
{
    /// <summary>
    /// Reference to the notification template used
    /// </summary>
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>
    /// Template name for easy reference
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// The channel used to send the notification
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Current status of the notification
    /// </summary>
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    /// <summary>
    /// Recipient information
    /// </summary>
    public NotificationRecipient Recipient { get; set; } = new();

    /// <summary>
    /// The actual content that was sent
    /// </summary>
    public NotificationContent Content { get; set; } = new();

    /// <summary>
    /// When the notification was sent (if successful)
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Maximum number of retries allowed
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Error message if the notification failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// External provider's response/message ID
    /// </summary>
    public string? ExternalMessageId { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Represents the recipient of a notification
/// </summary>
public class NotificationRecipient
{
    /// <summary>
    /// User ID if available
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Email address for email notifications
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Phone number for SMS notifications
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Device token for push notifications
    /// </summary>
    public string? DeviceToken { get; set; }

    /// <summary>
    /// Recipient's preferred language
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Recipient's timezone
    /// </summary>
    public string? TimeZone { get; set; }
}

/// <summary>
/// Represents the actual content of a notification
/// </summary>
public class NotificationContent
{
    /// <summary>
    /// Subject of the notification (for email)
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Body/message content
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Variables that were replaced in the template
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new();
}