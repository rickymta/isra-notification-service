namespace NotificationService.Application.Settings;

/// <summary>
/// Configuration settings for push notification service
/// </summary>
public class PushSettings
{
    public const string SectionName = "Push";

    /// <summary>
    /// FCM server key
    /// </summary>
    public string? FcmServerKey { get; set; }

    /// <summary>
    /// FCM project ID
    /// </summary>
    public string? FcmProjectId { get; set; }

    /// <summary>
    /// Path to FCM service account JSON file
    /// </summary>
    public string? FcmServiceAccountPath { get; set; }
}