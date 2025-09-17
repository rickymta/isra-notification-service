using NotificationService.Domain.Entities;

namespace NotificationService.Application.Interfaces;

/// <summary>
/// Interface for message publishing
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publish a notification request to the queue
    /// </summary>
    Task PublishNotificationAsync(NotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish a notification request with delay
    /// </summary>
    Task PublishNotificationWithDelayAsync(NotificationRequest request, TimeSpan delay, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish a retry notification request
    /// </summary>
    Task PublishRetryNotificationAsync(NotificationRequest request, int retryAttempt, CancellationToken cancellationToken = default);
}