namespace NotificationService.Domain.Enums;

/// <summary>
/// Represents the status of a notification
/// </summary>
public enum NotificationStatus
{
    Pending = 1,
    Processing = 2,
    Sent = 3,
    Failed = 4,
    Retrying = 5
}