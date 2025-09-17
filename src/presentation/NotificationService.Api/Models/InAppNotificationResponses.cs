namespace NotificationService.Api.Models;

/// <summary>
/// Response model for in-app notification
/// </summary>
public class InAppNotificationResponse
{
    /// <summary>
    /// Notification identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// User identifier
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
    /// Notification type
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Notification priority
    /// </summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>
    /// Additional notification data
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Action URL for click handling
    /// </summary>
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Action button text
    /// </summary>
    public string? ActionText { get; set; }

    /// <summary>
    /// Notification icon
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Sender avatar
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// Sender information
    /// </summary>
    public string? Sender { get; set; }

    /// <summary>
    /// Group identifier for grouping notifications
    /// </summary>
    public string? GroupId { get; set; }

    /// <summary>
    /// Show as toast notification
    /// </summary>
    public bool ShowToast { get; set; }

    /// <summary>
    /// Play notification sound
    /// </summary>
    public bool PlaySound { get; set; }

    /// <summary>
    /// Custom sound file
    /// </summary>
    public string? SoundFile { get; set; }

    /// <summary>
    /// Notification tags
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Whether notification has been read
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Whether notification has been delivered
    /// </summary>
    public bool IsDelivered { get; set; }

    /// <summary>
    /// When notification was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When notification was read
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// When notification was delivered
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Notification expiration time
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether notification is persistent
    /// </summary>
    public bool IsPersistent { get; set; }
}

/// <summary>
/// Response model for bulk notification creation
/// </summary>
public class BulkNotificationResponse
{
    /// <summary>
    /// Total number of target users
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// Number of successful notifications
    /// </summary>
    public int SuccessfulNotifications { get; set; }

    /// <summary>
    /// Number of failed notifications
    /// </summary>
    public int FailedNotifications { get; set; }

    /// <summary>
    /// List of created notification IDs
    /// </summary>
    public List<string> NotificationIds { get; set; } = new();
}

/// <summary>
/// Response model for paged notifications
/// </summary>
public class PagedNotificationResponse
{
    /// <summary>
    /// List of notifications
    /// </summary>
    public List<InAppNotificationResponse> Notifications { get; set; } = new();

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total count of notifications
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Whether there are more pages
    /// </summary>
    public bool HasNextPage => Page * PageSize < TotalCount;

    /// <summary>
    /// Whether there are previous pages
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}

/// <summary>
/// Response model for unread count
/// </summary>
public class UnreadCountResponse
{
    /// <summary>
    /// Number of unread notifications
    /// </summary>
    public int Count { get; set; }
}

/// <summary>
/// Response model for user notification preferences
/// </summary>
public class UserNotificationPreferenceResponse
{
    /// <summary>
    /// Preference identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// User identifier
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Enable in-app notifications
    /// </summary>
    public bool IsInAppEnabled { get; set; }

    /// <summary>
    /// Enable email notifications
    /// </summary>
    public bool IsEmailEnabled { get; set; }

    /// <summary>
    /// Enable SMS notifications
    /// </summary>
    public bool IsSmsEnabled { get; set; }

    /// <summary>
    /// Enable push notifications
    /// </summary>
    public bool IsPushEnabled { get; set; }

    /// <summary>
    /// Enable sound for notifications
    /// </summary>
    public bool IsSoundEnabled { get; set; }

    /// <summary>
    /// Show notification toast/popup
    /// </summary>
    public bool IsToastEnabled { get; set; }

    /// <summary>
    /// Notification frequency for digest emails
    /// </summary>
    public string EmailFrequency { get; set; } = string.Empty;

    /// <summary>
    /// Enable quiet hours
    /// </summary>
    public bool EnableQuietHours { get; set; }

    /// <summary>
    /// Quiet hours start time
    /// </summary>
    public TimeSpan? QuietHoursStart { get; set; }

    /// <summary>
    /// Quiet hours end time
    /// </summary>
    public TimeSpan? QuietHoursEnd { get; set; }

    /// <summary>
    /// Timezone for quiet hours
    /// </summary>
    public string TimeZone { get; set; } = string.Empty;

    /// <summary>
    /// Preferred language for notifications
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Custom notification settings per type
    /// </summary>
    public Dictionary<string, bool> TypeSettings { get; set; } = new();

    /// <summary>
    /// Channel preferences per notification type
    /// </summary>
    public Dictionary<string, List<string>> ChannelPreferences { get; set; } = new();

    /// <summary>
    /// Blocked keywords for filtering notifications
    /// </summary>
    public List<string> BlockedKeywords { get; set; } = new();

    /// <summary>
    /// Blocked users/senders
    /// </summary>
    public List<string> BlockedSenders { get; set; } = new();

    /// <summary>
    /// Maximum notifications per hour (rate limiting)
    /// </summary>
    public int MaxNotificationsPerHour { get; set; }

    /// <summary>
    /// Priority threshold - only show notifications above this priority
    /// </summary>
    public string MinimumPriority { get; set; } = string.Empty;

    /// <summary>
    /// Enable notification grouping
    /// </summary>
    public bool EnableGrouping { get; set; }

    /// <summary>
    /// Auto-mark as read after specified minutes
    /// </summary>
    public int? AutoMarkReadAfterMinutes { get; set; }

    /// <summary>
    /// Delete notifications after specified days
    /// </summary>
    public int? DeleteAfterDays { get; set; }

    /// <summary>
    /// Device-specific settings
    /// </summary>
    public Dictionary<string, object> DeviceSettings { get; set; } = new();

    /// <summary>
    /// When preferences were created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When preferences were last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Response model for connected users
/// </summary>
public class ConnectedUsersResponse
{
    /// <summary>
    /// List of connected user IDs
    /// </summary>
    public List<string> ConnectedUsers { get; set; } = new();

    /// <summary>
    /// Total count of connected users
    /// </summary>
    public int Count { get; set; }
}

/// <summary>
/// Response model for bulk in-app notification creation
/// </summary>
public class BulkInAppNotificationResponse
{
    /// <summary>
    /// Total number of notifications created
    /// </summary>
    public int TotalCreated { get; set; }

    /// <summary>
    /// List of created notifications
    /// </summary>
    public List<InAppNotificationResponse> Notifications { get; set; } = new();
}

/// <summary>
/// Response model for user notifications with pagination
/// </summary>
public class UserNotificationsResponse
{
    /// <summary>
    /// User identifier
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// List of notifications
    /// </summary>
    public List<InAppNotificationResponse> Notifications { get; set; } = new();

    /// <summary>
    /// Number of unread notifications
    /// </summary>
    public int UnreadCount { get; set; }

    /// <summary>
    /// Total count of notifications
    /// </summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// Response model for bulk operations
/// </summary>
public class BulkOperationResponse
{
    /// <summary>
    /// Number of successful operations
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Total number of requested operations
    /// </summary>
    public int TotalRequested { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
