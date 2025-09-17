namespace NotificationService.Domain.Enums;

/// <summary>
/// Represents the different channels through which notifications can be sent
/// </summary>
public enum NotificationChannel
{
    Email = 1,
    Sms = 2,
    Push = 3,
    InApp = 4
}