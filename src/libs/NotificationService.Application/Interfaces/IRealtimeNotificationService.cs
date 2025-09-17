using NotificationService.Domain.Entities;

namespace NotificationService.Application.Interfaces;

/// <summary>
/// Interface for real-time notification service using SignalR
/// </summary>
public interface IRealtimeNotificationService
{
    /// <summary>
    /// Send notification to a specific user
    /// </summary>
    /// <param name="notification">The notification to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if sent successfully</returns>
    Task<bool> SendToUserAsync(InAppNotification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send notification to multiple users
    /// </summary>
    /// <param name="userIds">List of user IDs</param>
    /// <param name="notification">The notification to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of users successfully notified</returns>
    Task<int> SendToUsersAsync(List<string> userIds, InAppNotification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send notification to a group
    /// </summary>
    /// <param name="groupName">Group name</param>
    /// <param name="notification">The notification to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if sent successfully</returns>
    Task<bool> SendToGroupAsync(string groupName, InAppNotification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send notification to all connected users
    /// </summary>
    /// <param name="notification">The notification to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if sent successfully</returns>
    Task<bool> SendToAllAsync(InAppNotification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add user to a group
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="groupName">Group name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if added successfully</returns>
    Task<bool> AddUserToGroupAsync(string userId, string groupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove user from a group
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="groupName">Group name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if removed successfully</returns>
    Task<bool> RemoveUserFromGroupAsync(string userId, string groupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of connected users
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of connected user IDs</returns>
    Task<List<string>> GetConnectedUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user is online
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user is online</returns>
    Task<bool> IsUserOnlineAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's connection IDs
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of connection IDs for the user</returns>
    Task<List<string>> GetUserConnectionsAsync(string userId, CancellationToken cancellationToken = default);
}
/// <summary>
/// Interface for in-app notification repository
/// </summary>
public interface IInAppNotificationRepository
{
    /// <summary>
    /// Create a new in-app notification
    /// </summary>
    /// <param name="notification">The notification to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created notification</returns>
    Task<InAppNotification> CreateAsync(InAppNotification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notification by ID
    /// </summary>
    /// <param name="id">Notification ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The notification or null if not found</returns>
    Task<InAppNotification?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notifications for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="unreadOnly">Whether to return only unread notifications</param>
    /// <param name="skip">Number of notifications to skip</param>
    /// <param name="take">Number of notifications to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of notifications and total count</returns>
    Task<(List<InAppNotification> Notifications, int TotalCount)> GetByUserIdAsync(
        string userId, 
        bool unreadOnly = false, 
        int skip = 0, 
        int take = 20, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update notification
    /// </summary>
    /// <param name="notification">The notification to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated notification</returns>
    Task<InAppNotification> UpdateAsync(InAppNotification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete notification
    /// </summary>
    /// <param name="id">Notification ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unread count for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Unread notification count</returns>
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark notifications as read
    /// </summary>
    /// <param name="notificationIds">List of notification IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of notifications marked as read</returns>
    Task<int> MarkAsReadAsync(List<string> notificationIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up expired notifications
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of notifications deleted</returns>
    Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for user notification preferences repository
/// </summary>
public interface IUserNotificationPreferenceRepository
{
    /// <summary>
    /// Get user preferences
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user preferences</returns>
    Task<List<UserNotificationPreference>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create or update user preference
    /// </summary>
    /// <param name="preference">The preference to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The saved preference</returns>
    Task<UserNotificationPreference> UpsertAsync(UserNotificationPreference preference, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete user preference
    /// </summary>
    /// <param name="id">Preference ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
