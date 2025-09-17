using System.ComponentModel.DataAnnotations;

namespace NotificationService.Api.Models;

/// <summary>
/// Request model for creating bulk notifications
/// </summary>
public class BulkCreateInAppNotificationRequest
{
    /// <summary>
    /// List of notifications to create
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<CreateInAppNotificationRequest> Notifications { get; set; } = new();
}

/// <summary>
/// Request model for marking multiple notifications as read
/// </summary>
public class BulkMarkAsReadRequest
{
    /// <summary>
    /// List of notification IDs to mark as read
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<string> NotificationIds { get; set; } = new();
}

/// <summary>
/// Request model for sending real-time notification to user
/// </summary>
public class SendRealtimeNotificationRequest
{
    /// <summary>
    /// Target user identifier
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Notification message content
    /// </summary>
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional data
    /// </summary>
    public object? Data { get; set; }
}

/// <summary>
/// Request model for broadcasting real-time notification
/// </summary>
public class BroadcastRealtimeNotificationRequest
{
    /// <summary>
    /// Notification message content
    /// </summary>
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional data
    /// </summary>
    public object? Data { get; set; }
}
