using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Entities;

/// <summary>
/// Represents a bulk notification request for sending notifications to multiple recipients
/// </summary>
public class BulkNotificationRequest : BaseEntity
{
    /// <summary>
    /// Template ID to use for the bulk notification
    /// </summary>
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>
    /// The notification channel (Email, SMS, Push)
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// List of recipients with their specific variables
    /// </summary>
    public List<BulkRecipient> Recipients { get; set; } = new();

    /// <summary>
    /// Common variables applied to all recipients
    /// </summary>
    public Dictionary<string, object> CommonVariables { get; set; } = new();

    /// <summary>
    /// Subject template override (optional, if different from template)
    /// </summary>
    public string? SubjectOverride { get; set; }

    /// <summary>
    /// Body template override (optional, if different from template)
    /// </summary>
    public string? BodyOverride { get; set; }

    /// <summary>
    /// Batch size for processing recipients
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Maximum concurrent batches to process
    /// </summary>
    public int MaxConcurrentBatches { get; set; } = 5;

    /// <summary>
    /// Delay between batches in milliseconds
    /// </summary>
    public int BatchDelayMs { get; set; } = 1000;

    /// <summary>
    /// Priority level for the bulk notification
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Whether to continue processing if some notifications fail
    /// </summary>
    public bool ContinueOnError { get; set; } = true;

    /// <summary>
    /// Status of the bulk notification processing
    /// </summary>
    public BulkNotificationStatus Status { get; set; } = BulkNotificationStatus.Pending;

    /// <summary>
    /// Total number of recipients
    /// </summary>
    public int TotalRecipients { get; set; }

    /// <summary>
    /// Number of successfully processed notifications
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed notifications
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Number of currently processing notifications
    /// </summary>
    public int ProcessingCount { get; set; }

    /// <summary>
    /// When the bulk processing started
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the bulk processing completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Error message if the entire bulk operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public decimal ProgressPercentage => TotalRecipients > 0 
        ? Math.Round((decimal)(SuccessCount + FailureCount) / TotalRecipients * 100, 2) 
        : 0;
}

/// <summary>
/// Represents an individual recipient in a bulk notification
/// </summary>
public class BulkRecipient
{
    /// <summary>
    /// Unique identifier for this recipient
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Recipient address (email, phone number, or device token)
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Recipient's name (optional)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Language preference for this recipient
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Variables specific to this recipient
    /// </summary>
    public Dictionary<string, object> Variables { get; set; } = new();

    /// <summary>
    /// Metadata for this recipient
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Processing status for this recipient
    /// </summary>
    public RecipientStatus Status { get; set; } = RecipientStatus.Pending;

    /// <summary>
    /// Error message if processing failed for this recipient
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When this recipient was processed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Number of retry attempts for this recipient
    /// </summary>
    public int RetryCount { get; set; }
}

/// <summary>
/// Status of the bulk notification processing
/// </summary>
public enum BulkNotificationStatus
{
    /// <summary>
    /// Bulk notification is queued for processing
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Bulk notification is currently being processed
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Bulk notification completed successfully
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Bulk notification completed with some failures
    /// </summary>
    CompletedWithErrors = 3,

    /// <summary>
    /// Bulk notification failed completely
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Bulk notification was cancelled
    /// </summary>
    Cancelled = 5
}

/// <summary>
/// Status of individual recipient processing
/// </summary>
public enum RecipientStatus
{
    /// <summary>
    /// Recipient is pending processing
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Recipient is currently being processed
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Recipient notification sent successfully
    /// </summary>
    Success = 2,

    /// <summary>
    /// Recipient notification failed
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Recipient was skipped (e.g., invalid address)
    /// </summary>
    Skipped = 4
}
