using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Entities;

/// <summary>
/// Represents an in-app notification for real-time delivery via SignalR
/// </summary>
public class InAppNotification : BaseEntity
{
    /// <summary>
    /// Target user ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Notification title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Notification message content
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Notification type/category
    /// </summary>
    public string Type { get; set; } = "info";

    /// <summary>
    /// Priority level
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Additional data payload
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Action URL or deep link
    /// </summary>
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Action button text
    /// </summary>
    public string? ActionText { get; set; }

    /// <summary>
    /// Icon URL or icon name
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Avatar URL for sender
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// Whether the notification has been read
    /// </summary>
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// Whether the notification has been delivered
    /// </summary>
    public bool IsDelivered { get; set; } = false;

    /// <summary>
    /// When the notification was read
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// When the notification was delivered
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Expiration time for the notification
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Tags for categorizing notifications
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Sender information
    /// </summary>
    public NotificationSender? Sender { get; set; }

    /// <summary>
    /// Group ID for related notifications
    /// </summary>
    public string? GroupId { get; set; }

    /// <summary>
    /// Whether this notification should be persistent (stored in DB)
    /// </summary>
    public bool IsPersistent { get; set; } = true;

    /// <summary>
    /// Whether this notification should show a toast/popup
    /// </summary>
    public bool ShowToast { get; set; } = true;

    /// <summary>
    /// Whether this notification should play a sound
    /// </summary>
    public bool PlaySound { get; set; } = false;

    /// <summary>
    /// Custom sound file name
    /// </summary>
    public string? SoundFile { get; set; }
}

/// <summary>
/// Represents notification sender information
/// </summary>
public class NotificationSender
{
    /// <summary>
    /// Sender ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Sender name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Sender avatar URL
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// Sender type (user, system, service, etc.)
    /// </summary>
    public string Type { get; set; } = "system";
}
