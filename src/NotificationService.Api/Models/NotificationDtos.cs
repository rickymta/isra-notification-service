using NotificationService.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace NotificationService.Api.Models;

/// <summary>
/// Request model for sending notifications
/// </summary>
public class SendNotificationRequest
{
    /// <summary>
    /// Template ID to use (optional if TemplateName is provided)
    /// </summary>
    public string? TemplateId { get; set; }

    /// <summary>
    /// Template name to use (optional if TemplateId is provided)
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// Notification channel
    /// </summary>
    [Required]
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Recipient information
    /// </summary>
    [Required]
    public NotificationRecipientDto Recipient { get; set; } = new();

    /// <summary>
    /// Variables to replace in the template
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new();

    /// <summary>
    /// When to send the notification (null = immediate)
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// Priority of the notification (1 = highest, 5 = lowest)
    /// </summary>
    [Range(1, 5)]
    public int Priority { get; set; } = 3;

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Recipient DTO for API requests
/// </summary>
public class NotificationRecipientDto
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
/// Response model for notification send requests
/// </summary>
public class SendNotificationResponse
{
    /// <summary>
    /// Notification ID for tracking
    /// </summary>
    public string NotificationId { get; set; } = string.Empty;

    /// <summary>
    /// Whether the request was accepted
    /// </summary>
    public bool IsAccepted { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Estimated processing time (for scheduled notifications)
    /// </summary>
    public DateTime? EstimatedProcessingTime { get; set; }
}

/// <summary>
/// Response model for notification status queries
/// </summary>
public class NotificationStatusResponse
{
    /// <summary>
    /// Notification ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Template name used
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Channel used
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Current status
    /// </summary>
    public NotificationStatus Status { get; set; }

    /// <summary>
    /// Recipient information
    /// </summary>
    public NotificationRecipientDto Recipient { get; set; } = new();

    /// <summary>
    /// When the notification was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the notification was sent (if successful)
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// External provider's message ID
    /// </summary>
    public string? ExternalMessageId { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}