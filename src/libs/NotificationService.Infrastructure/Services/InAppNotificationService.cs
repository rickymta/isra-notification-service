using Microsoft.Extensions.Logging;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using System.Security.Claims;

namespace NotificationService.Infrastructure.Services;

public class InAppNotificationService : IInAppNotificationService
{
    private readonly IInAppNotificationRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly IRealtimeNotificationService _realtimeService;
    private readonly ILogger<InAppNotificationService> _logger;

    public InAppNotificationService(
        IInAppNotificationRepository repository,
        ICacheService cacheService,
        IRealtimeNotificationService realtimeService,
        ILogger<InAppNotificationService> logger)
    {
        _repository = repository;
        _cacheService = cacheService;
        _realtimeService = realtimeService;
        _logger = logger;
    }

    public async Task<InAppNotification> CreateNotificationAsync(
        string userId,
        string title,
        string message,
        NotificationChannel channel = NotificationChannel.InApp,
        object? data = null,
        DateTime? expiresAt = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = new InAppNotification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Data = data as Dictionary<string, object> ?? new Dictionary<string, object>(),
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.CreateAsync(notification, cancellationToken);

            // Send real-time notification
            await _realtimeService.SendToUserAsync(notification, cancellationToken);

            _logger.LogInformation("Created in-app notification {NotificationId} for user {UserId}", 
                notification.Id, userId);

            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<InAppNotification>> CreateBulkNotificationAsync(
        IEnumerable<string> userIds,
        string title,
        string message,
        NotificationChannel channel = NotificationChannel.InApp,
        object? data = null,
        DateTime? expiresAt = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notifications = userIds.Select(userId => new InAppNotification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Data = data as Dictionary<string, object> ?? new Dictionary<string, object>(),
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            // Create individual notifications
            var createdNotifications = new List<InAppNotification>();
            foreach (var notification in notifications)
            {
                var created = await _repository.CreateAsync(notification, cancellationToken);
                createdNotifications.Add(created);
            }

            // Send real-time notifications
            foreach (var notification in createdNotifications)
            {
                await _realtimeService.SendToUserAsync(notification, cancellationToken);
            }

            _logger.LogInformation("Created {Count} bulk notifications", createdNotifications.Count);

            return createdNotifications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create bulk notifications");
            throw;
        }
    }

    public async Task<InAppNotification?> GetNotificationAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"notification:{id}";
            var cached = await _cacheService.GetAsync<InAppNotification>(cacheKey, cancellationToken);
            if (cached != null)
                return cached;

            var notification = await _repository.GetByIdAsync(id, cancellationToken);
            if (notification != null)
            {
                await _cacheService.SetAsync(cacheKey, notification, TimeSpan.FromMinutes(30), cancellationToken);
            }

            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification {NotificationId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<InAppNotification>> GetUserNotificationsAsync(
        string userId,
        int page = 1,
        int pageSize = 20,
        bool? isRead = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"user_notifications:{userId}:{page}:{pageSize}:{isRead}";
            var cached = await _cacheService.GetAsync<IEnumerable<InAppNotification>>(cacheKey, cancellationToken);
            if (cached != null)
                return cached;

            var skip = (page - 1) * pageSize;
            var unreadOnly = isRead.HasValue ? !isRead.Value : false;
            
            var (notifications, _) = await _repository.GetByUserIdAsync(userId, unreadOnly, skip, pageSize, cancellationToken);
            
            await _cacheService.SetAsync(cacheKey, notifications, TimeSpan.FromMinutes(5), cancellationToken);

            return notifications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notifications for user {UserId}", userId);
            throw;
        }
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"unread_count:{userId}";
            var cached = await _cacheService.GetAsync<int?>(cacheKey, cancellationToken);
            if (cached.HasValue)
                return cached.Value;

            var count = await _repository.GetUnreadCountAsync(userId, cancellationToken);
            
            await _cacheService.SetAsync(cacheKey, count, TimeSpan.FromMinutes(2), cancellationToken);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unread count for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _repository.GetByIdAsync(notificationId, cancellationToken);
            if (notification == null)
                return false;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(notification, cancellationToken);

            // Clear related cache
            await InvalidateUserCacheAsync(notification.UserId, cancellationToken);

            _logger.LogInformation("Marked notification {NotificationId} as read", notificationId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification {NotificationId} as read", notificationId);
            throw;
        }
    }

    public async Task<bool> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get unread notifications
            var (unreadNotifications, _) = await _repository.GetByUserIdAsync(userId, true, 0, int.MaxValue, cancellationToken);
            
            if (!unreadNotifications.Any())
                return true;

            // Mark as read
            var notificationIds = unreadNotifications.Select(n => n.Id).ToList();
            var result = await _repository.MarkAsReadAsync(notificationIds, cancellationToken);

            // Clear user cache
            await InvalidateUserCacheAsync(userId, cancellationToken);

            _logger.LogInformation("Marked all notifications as read for user {UserId}", userId);

            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark all notifications as read for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> DeleteNotificationAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _repository.GetByIdAsync(notificationId, cancellationToken);
            if (notification == null)
                return false;

            await _repository.DeleteAsync(notificationId, cancellationToken);

            // Clear related cache
            await InvalidateUserCacheAsync(notification.UserId, cancellationToken);

            _logger.LogInformation("Deleted notification {NotificationId}", notificationId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete notification {NotificationId}", notificationId);
            throw;
        }
    }

    public async Task<bool> DeleteAllUserNotificationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all user notifications
            var (userNotifications, _) = await _repository.GetByUserIdAsync(userId, false, 0, int.MaxValue, cancellationToken);
            
            if (!userNotifications.Any())
                return true;

            // Delete notifications one by one
            var deleteCount = 0;
            foreach (var notification in userNotifications)
            {
                var deleted = await _repository.DeleteAsync(notification.Id, cancellationToken);
                if (deleted) deleteCount++;
            }

            // Clear user cache
            await InvalidateUserCacheAsync(userId, cancellationToken);

            _logger.LogInformation("Deleted {Count} notifications for user {UserId}", deleteCount, userId);

            return deleteCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete all notifications for user {UserId}", userId);
            throw;
        }
    }

    public async Task SendRealtimeNotificationAsync(
        string userId,
        string title,
        string message,
        object? data = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = new InAppNotification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Data = data as Dictionary<string, object> ?? new Dictionary<string, object>(),
                CreatedAt = DateTime.UtcNow
            };

            await _realtimeService.SendToUserAsync(notification, cancellationToken);

            _logger.LogInformation("Sent real-time notification to user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send real-time notification to user {UserId}", userId);
            throw;
        }
    }

    public async Task BroadcastNotificationAsync(
        string title,
        string message,
        object? data = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = new InAppNotification
            {
                Title = title,
                Message = message,
                Data = data as Dictionary<string, object> ?? new Dictionary<string, object>(),
                CreatedAt = DateTime.UtcNow
            };

            await _realtimeService.SendToAllAsync(notification, cancellationToken);

            _logger.LogInformation("Broadcasted notification to all users");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast notification");
            throw;
        }
    }

    public async Task<bool> UpdateNotificationPreferencesAsync(
        string userId,
        bool enableToast,
        bool enableSound,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // This would typically update user preferences in a separate preferences table
            // For now, just return true as this is a placeholder
            _logger.LogInformation("Updated notification preferences for user {UserId}", userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update preferences for user {UserId}", userId);
            throw;
        }
    }

    public async Task CleanupExpiredNotificationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var deletedCount = await _repository.CleanupExpiredAsync(cancellationToken);

            _logger.LogInformation("Cleaned up {Count} expired notifications", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired notifications");
            throw;
        }
    }

    private async Task InvalidateUserCacheAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            // Clear unread count
            await _cacheService.RemoveAsync($"unread_count:{userId}", cancellationToken);

            // Clear user notifications cache (basic pattern)
            await _cacheService.RemoveByPatternAsync($"user_notifications:{userId}:*", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cache for user {UserId}", userId);
        }
    }
}
