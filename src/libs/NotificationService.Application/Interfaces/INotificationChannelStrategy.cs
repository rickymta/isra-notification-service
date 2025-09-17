using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Application.Interfaces;

/// <summary>
/// Strategy interface for notification channels
/// </summary>
public interface INotificationChannelStrategy
{
    /// <summary>
    /// The channel this strategy handles
    /// </summary>
    NotificationChannel Channel { get; }

    /// <summary>
    /// Send notification through this channel
    /// </summary>
    Task<NotificationResult> SendAsync(NotificationContent content, NotificationRecipient recipient, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate recipient for this channel
    /// </summary>
    bool ValidateRecipient(NotificationRecipient recipient);
}

/// <summary>
/// Factory for creating notification channel strategies
/// </summary>
public interface INotificationChannelFactory
{
    /// <summary>
    /// Get strategy for specific channel
    /// </summary>
    INotificationChannelStrategy GetStrategy(NotificationChannel channel);
}