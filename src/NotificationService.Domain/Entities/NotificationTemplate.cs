using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Entities;

/// <summary>
/// Represents a notification template that can be used to generate notifications
/// Supports multiple languages for internationalization
/// </summary>
public class NotificationTemplate : BaseEntity
{
    /// <summary>
    /// Template name/identifier
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The channel this template is designed for
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Subject template (for email notifications)
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Body template content
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Language code (e.g., "en", "vi", "fr")
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Template variables that can be replaced (e.g., {{userName}}, {{orderNumber}})
    /// </summary>
    public List<string> Variables { get; set; } = new();

    /// <summary>
    /// Whether this template is active and can be used
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Template version for tracking changes
    /// </summary>
    public int Version { get; set; } = 1;
}