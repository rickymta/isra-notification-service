using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Application.Interfaces;

public interface IInAppNotificationService
{
    // Core notification operations
    Task<InAppNotification> CreateNotificationAsync(
        string userId,
        string title,
        string message,
        NotificationChannel channel = NotificationChannel.InApp,
        object? data = null,
        DateTime? expiresAt = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<InAppNotification>> CreateBulkNotificationAsync(
        IEnumerable<string> userIds,
        string title,
        string message,
        NotificationChannel channel = NotificationChannel.InApp,
        object? data = null,
        DateTime? expiresAt = null,
        CancellationToken cancellationToken = default);

    // Retrieval operations
    Task<InAppNotification?> GetNotificationAsync(string id, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<InAppNotification>> GetUserNotificationsAsync(
        string userId,
        int page = 1,
        int pageSize = 20,
        bool? isRead = null,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);

    // Status operations
    Task<bool> MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default);
    Task<bool> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);

    // Delete operations
    Task<bool> DeleteNotificationAsync(string notificationId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAllUserNotificationsAsync(string userId, CancellationToken cancellationToken = default);

    // Real-time operations
    Task SendRealtimeNotificationAsync(
        string userId,
        string title,
        string message,
        object? data = null,
        CancellationToken cancellationToken = default);

    Task BroadcastNotificationAsync(
        string title,
        string message,
        object? data = null,
        CancellationToken cancellationToken = default);

    // User preferences
    Task<bool> UpdateNotificationPreferencesAsync(
        string userId,
        bool enableToast,
        bool enableSound,
        CancellationToken cancellationToken = default);

    // Maintenance
    Task CleanupExpiredNotificationsAsync(CancellationToken cancellationToken = default);
}
