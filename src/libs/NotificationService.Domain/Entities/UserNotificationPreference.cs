using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Entities;

/// <summary>
/// Represents user preferences for different notification channels
/// </summary>
public class UserNotificationPreference : BaseEntity
{
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Notification channel
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Whether this channel is enabled for the user
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether to show toast notifications (for in-app)
    /// </summary>
    public bool EnableToast { get; set; } = true;

    /// <summary>
    /// Whether to play sound for notifications
    /// </summary>
    public bool EnableSound { get; set; } = true;

    /// <summary>
    /// Quiet hours start time (for push notifications)
    /// </summary>
    public TimeSpan? QuietHoursStart { get; set; }

    /// <summary>
    /// Quiet hours end time (for push notifications)
    /// </summary>
    public TimeSpan? QuietHoursEnd { get; set; }

    /// <summary>
    /// Frequency preference (immediate, daily digest, weekly digest)
    /// </summary>
    public string Frequency { get; set; } = "immediate";

    /// <summary>
    /// Custom settings as JSON
    /// </summary>
    public Dictionary<string, object> Settings { get; set; } = new();
}
