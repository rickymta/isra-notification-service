using NotificationService.Domain.Entities;

namespace NotificationService.Application.Interfaces;

/// <summary>
/// Interface for email notification service
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send an email notification
    /// </summary>
    Task<NotificationResult> SendEmailAsync(NotificationContent content, NotificationRecipient recipient, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate email recipient
    /// </summary>
    bool ValidateRecipient(NotificationRecipient recipient);
}

/// <summary>
/// Interface for SMS notification service
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Send an SMS notification
    /// </summary>
    Task<NotificationResult> SendSmsAsync(NotificationContent content, NotificationRecipient recipient, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate SMS recipient
    /// </summary>
    bool ValidateRecipient(NotificationRecipient recipient);
}

/// <summary>
/// Interface for push notification service
/// </summary>
public interface IPushService
{
    /// <summary>
    /// Send a push notification
    /// </summary>
    Task<NotificationResult> SendPushAsync(NotificationContent content, NotificationRecipient recipient, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate push notification recipient
    /// </summary>
    bool ValidateRecipient(NotificationRecipient recipient);
}

/// <summary>
/// Result of a notification sending operation
/// </summary>
public class NotificationResult
{
    /// <summary>
    /// Whether the notification was sent successfully
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if sending failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// External provider's message ID
    /// </summary>
    public string? ExternalMessageId { get; set; }

    /// <summary>
    /// Additional metadata from the provider
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Create a successful result
    /// </summary>
    public static NotificationResult Success(string? externalMessageId = null)
    {
        return new NotificationResult
        {
            IsSuccess = true,
            ExternalMessageId = externalMessageId
        };
    }

    /// <summary>
    /// Create a failed result
    /// </summary>
    public static NotificationResult Failure(string errorMessage)
    {
        return new NotificationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}