using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Application.Interfaces;

/// <summary>
/// Repository interface for notification history
/// </summary>
public interface INotificationHistoryRepository
{
    /// <summary>
    /// Get notification history by ID
    /// </summary>
    Task<NotificationHistory?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notification history by external message ID
    /// </summary>
    Task<NotificationHistory?> GetByExternalMessageIdAsync(string externalMessageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notification history by user ID
    /// </summary>
    Task<IEnumerable<NotificationHistory>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notification history by status
    /// </summary>
    Task<IEnumerable<NotificationHistory>> GetByStatusAsync(NotificationStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notification history by date range
    /// </summary>
    Task<IEnumerable<NotificationHistory>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get failed notifications that need retry
    /// </summary>
    Task<IEnumerable<NotificationHistory>> GetFailedForRetryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new notification history record
    /// </summary>
    Task<NotificationHistory> CreateAsync(NotificationHistory history, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing notification history record
    /// </summary>
    Task<NotificationHistory> UpdateAsync(NotificationHistory history, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update notification status
    /// </summary>
    Task<bool> UpdateStatusAsync(string id, NotificationStatus status, string? errorMessage = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increment retry count
    /// </summary>
    Task<bool> IncrementRetryCountAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete old notification history records
    /// </summary>
    Task<long> DeleteOldRecordsAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}