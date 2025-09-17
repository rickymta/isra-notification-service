using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Entities;

/// <summary>
/// Represents a notification request that will be processed
/// </summary>
public class NotificationRequest
{
    /// <summary>
    /// Unique identifier for this notification request
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Template identifier to use for this notification
    /// </summary>
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>
    /// Alternative: Template name instead of ID
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// Channel to send the notification through
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Recipient information
    /// </summary>
    public NotificationRecipient Recipient { get; set; } = new();

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
    public int Priority { get; set; } = 3;

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// When this request was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}