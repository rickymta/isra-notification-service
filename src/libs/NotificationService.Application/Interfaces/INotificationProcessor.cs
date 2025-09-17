using NotificationService.Domain.Entities;

namespace NotificationService.Application.Interfaces;

/// <summary>
/// Interface for processing notification requests
/// </summary>
public interface INotificationProcessor
{
    /// <summary>
    /// Process a notification request
    /// </summary>
    Task ProcessNotificationAsync(NotificationRequest request, CancellationToken cancellationToken = default);
}