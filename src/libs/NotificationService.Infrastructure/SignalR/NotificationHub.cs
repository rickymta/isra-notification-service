using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using System.Security.Claims;

namespace NotificationService.Infrastructure.SignalR;

/// <summary>
/// SignalR Hub for real-time notifications
/// </summary>
[Authorize] // Requires authentication
public class NotificationHub : Hub
{
    private readonly IConnectionManager _connectionManager;
    private readonly IInAppNotificationService _notificationService;
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(
        IConnectionManager connectionManager,
        IInAppNotificationService notificationService,
        ILogger<NotificationHub> logger)
    {
        _connectionManager = connectionManager;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        try
        {
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await _connectionManager.AddConnectionAsync(userId, Context.ConnectionId);
                
                // Join user to their personal group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
                
                _logger.LogInformation("User {UserId} connected with connection {ConnectionId}", userId, Context.ConnectionId);
                
                // Notify user about their connection status
                await Clients.Caller.SendAsync("ConnectionEstablished", new
                {
                    UserId = userId,
                    ConnectionId = Context.ConnectionId,
                    ConnectedAt = DateTime.UtcNow
                });

                // Send unread notification count
                var unreadCount = await _notificationService.GetUnreadCountAsync(userId);
                await Clients.Caller.SendAsync("UnreadCountUpdate", unreadCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnConnectedAsync for connection {ConnectionId}", Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await _connectionManager.RemoveConnectionAsync(userId, Context.ConnectionId);
                
                _logger.LogInformation("User {UserId} disconnected from connection {ConnectionId}", 
                    userId, Context.ConnectionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnDisconnectedAsync for connection {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a group
    /// </summary>
    /// <param name="groupName">Group name to join</param>
    public async Task JoinGroup(string groupName)
    {
        try
        {
            var userId = GetUserId();
            
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(groupName))
            {
                await Clients.Caller.SendAsync("Error", "Invalid user ID or group name");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await _connectionManager.AddUserToGroupAsync(userId, groupName);
            
            _logger.LogInformation("User {UserId} joined group {GroupName}", userId, groupName);
            
            await Clients.Caller.SendAsync("GroupJoined", new
            {
                GroupName = groupName,
                JoinedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining group {GroupName} for user {UserId}", groupName, GetUserId());
            await Clients.Caller.SendAsync("Error", "Failed to join group");
        }
    }

    /// <summary>
    /// Leave a group
    /// </summary>
    /// <param name="groupName">Group name to leave</param>
    public async Task LeaveGroup(string groupName)
    {
        try
        {
            var userId = GetUserId();
            
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(groupName))
            {
                await Clients.Caller.SendAsync("Error", "Invalid user ID or group name");
                return;
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            await _connectionManager.RemoveUserFromGroupAsync(userId, groupName);
            
            _logger.LogInformation("User {UserId} left group {GroupName}", userId, groupName);
            
            await Clients.Caller.SendAsync("GroupLeft", new
            {
                GroupName = groupName,
                LeftAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving group {GroupName} for user {UserId}", groupName, GetUserId());
            await Clients.Caller.SendAsync("Error", "Failed to leave group");
        }
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    public async Task MarkNotificationAsRead(string notificationId)
    {
        try
        {
            var userId = GetUserId();
            
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(notificationId))
            {
                await Clients.Caller.SendAsync("Error", "Invalid user ID or notification ID");
                return;
            }

            var success = await _notificationService.MarkAsReadAsync(notificationId);
            
            if (success)
            {
                // Send updated unread count
                var unreadCount = await _notificationService.GetUnreadCountAsync(userId);
                await Clients.Caller.SendAsync("UnreadCountUpdate", unreadCount);
                
                await Clients.Caller.SendAsync("NotificationMarkedAsRead", new
                {
                    NotificationId = notificationId,
                    MarkedAt = DateTime.UtcNow
                });
                
                _logger.LogInformation("Notification {NotificationId} marked as read by user {UserId}", 
                    notificationId, userId);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Failed to mark notification as read");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read for user {UserId}", 
                notificationId, GetUserId());
            await Clients.Caller.SendAsync("Error", "Failed to mark notification as read");
        }
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    public async Task MarkAllNotificationsAsRead()
    {
        try
        {
            var userId = GetUserId();
            
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.SendAsync("Error", "Invalid user ID");
                return;
            }

            var markedCount = await _notificationService.MarkAllAsReadAsync(userId);
            
            await Clients.Caller.SendAsync("AllNotificationsMarkedAsRead", new
            {
                MarkedCount = markedCount,
                MarkedAt = DateTime.UtcNow
            });
            
            // Send updated unread count (should be 0)
            await Clients.Caller.SendAsync("UnreadCountUpdate", 0);
            
            _logger.LogInformation("All notifications marked as read for user {UserId}, count: {Count}", 
                userId, markedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", GetUserId());
            await Clients.Caller.SendAsync("Error", "Failed to mark all notifications as read");
        }
    }

    /// <summary>
    /// Get user notifications
    /// </summary>
    /// <param name="skip">Number of notifications to skip</param>
    /// <param name="take">Number of notifications to take</param>
    /// <param name="unreadOnly">Whether to return only unread notifications</param>
    public async Task GetNotifications(int skip = 0, int take = 20, bool unreadOnly = false)
    {
        try
        {
            var userId = GetUserId();
            
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.SendAsync("Error", "Invalid user ID");
                return;
            }

            var page = skip / take + 1;
            var notifications = await _notificationService.GetUserNotificationsAsync(
                userId, page, take, unreadOnly ? false : (bool?)null);
            
            await Clients.Caller.SendAsync("NotificationsList", new
            {
                Notifications = notifications,
                TotalCount = notifications.Count(), // Simplified for now
                Skip = skip,
                Take = take,
                UnreadOnly = unreadOnly
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for user {UserId}", GetUserId());
            await Clients.Caller.SendAsync("Error", "Failed to get notifications");
        }
    }

    /// <summary>
    /// Send typing indicator
    /// </summary>
    /// <param name="groupName">Group name where typing is occurring</param>
    /// <param name="isTyping">Whether user is typing</param>
    public async Task SendTypingIndicator(string groupName, bool isTyping)
    {
        try
        {
            var userId = GetUserId();
            
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(groupName))
                return;

            await Clients.OthersInGroup(groupName).SendAsync("TypingIndicator", new
            {
                UserId = userId,
                GroupName = groupName,
                IsTyping = isTyping,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending typing indicator for user {UserId} in group {GroupName}", 
                GetUserId(), groupName);
        }
    }

    /// <summary>
    /// Ping to keep connection alive
    /// </summary>
    public async Task Ping()
    {
        await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
    }

    /// <summary>
    /// Get current user ID from claims
    /// </summary>
    /// <returns>User ID or empty string if not found</returns>
    private string GetUserId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               Context.User?.FindFirst("sub")?.Value ?? 
               Context.User?.FindFirst("userId")?.Value ?? 
               string.Empty;
    }
}

/// <summary>
/// Interface for managing SignalR connections
/// </summary>
public interface IConnectionManager
{
    /// <summary>
    /// Add a connection for a user
    /// </summary>
    Task AddConnectionAsync(string userId, string connectionId);

    /// <summary>
    /// Remove a connection for a user
    /// </summary>
    Task RemoveConnectionAsync(string userId, string connectionId);

    /// <summary>
    /// Get all connections for a user
    /// </summary>
    Task<List<string>> GetUserConnectionsAsync(string userId);

    /// <summary>
    /// Check if user is online
    /// </summary>
    Task<bool> IsUserOnlineAsync(string userId);

    /// <summary>
    /// Get all connected users
    /// </summary>
    Task<List<string>> GetConnectedUsersAsync();

    /// <summary>
    /// Add user to a group
    /// </summary>
    Task AddUserToGroupAsync(string userId, string groupName);

    /// <summary>
    /// Remove user from a group
    /// </summary>
    Task RemoveUserFromGroupAsync(string userId, string groupName);

    /// <summary>
    /// Get users in a group
    /// </summary>
    Task<List<string>> GetGroupUsersAsync(string groupName);

    /// <summary>
    /// Get groups for a user
    /// </summary>
    Task<List<string>> GetUserGroupsAsync(string userId);
}
