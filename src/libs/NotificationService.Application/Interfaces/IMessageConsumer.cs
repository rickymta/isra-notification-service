using NotificationService.Domain.Entities;

namespace NotificationService.Application.Interfaces;

/// <summary>
/// Interface for message consumption
/// </summary>
public interface IMessageConsumer
{
    /// <summary>
    /// Start consuming messages from the queue
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop consuming messages
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}