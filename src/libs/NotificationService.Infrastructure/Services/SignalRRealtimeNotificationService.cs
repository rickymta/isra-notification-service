using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.SignalR;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Real-time notification service implementation using SignalR
/// </summary>
public class SignalRRealtimeNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<SignalRRealtimeNotificationService> _logger;

    public SignalRRealtimeNotificationService(
        IHubContext<NotificationHub> hubContext,
        IConnectionManager connectionManager,
        ILogger<SignalRRealtimeNotificationService> logger)
    {
        _hubContext = hubContext;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public async Task<bool> SendToUserAsync(InAppNotification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(notification.UserId))
            {
                _logger.LogWarning("Cannot send notification - UserId is empty");
                return false;
            }

            // Check if user is online
            var isOnline = await _connectionManager.IsUserOnlineAsync(notification.UserId);
            
            if (!isOnline)
            {
                _logger.LogDebug("User {UserId} is not online, notification will not be sent via SignalR", notification.UserId);
                return false;
            }

            // Send to user's personal group
            var userGroup = $"user_{notification.UserId}";
            await _hubContext.Clients.Group(userGroup).SendAsync("ReceiveNotification", new
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                Priority = notification.Priority,
                Data = notification.Data,
                ActionUrl = notification.ActionUrl,
                ActionText = notification.ActionText,
                Icon = notification.Icon,
                Avatar = notification.Avatar,
                Sender = notification.Sender,
                GroupId = notification.GroupId,
                ShowToast = notification.ShowToast,
                PlaySound = notification.PlaySound,
                SoundFile = notification.SoundFile,
                Tags = notification.Tags,
                CreatedAt = notification.CreatedAt,
                ExpiresAt = notification.ExpiresAt
            }, cancellationToken);

            // Update delivered status
            notification.IsDelivered = true;
            notification.DeliveredAt = DateTime.UtcNow;

            _logger.LogInformation("Notification {NotificationId} sent to user {UserId} via SignalR", 
                notification.Id, notification.UserId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification {NotificationId} to user {UserId}", 
                notification.Id, notification.UserId);
            return false;
        }
    }

    public async Task<int> SendToUsersAsync(List<string> userIds, InAppNotification notification, CancellationToken cancellationToken = default)
    {
        var successCount = 0;

        foreach (var userId in userIds)
        {
            try
            {
                // Create a copy for each user
                var userNotification = new InAppNotification
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Title = notification.Title,
                    Message = notification.Message,
                    Type = notification.Type,
                    Priority = notification.Priority,
                    Data = new Dictionary<string, object>(notification.Data),
                    ActionUrl = notification.ActionUrl,
                    ActionText = notification.ActionText,
                    Icon = notification.Icon,
                    Avatar = notification.Avatar,
                    Sender = notification.Sender,
                    GroupId = notification.GroupId,
                    ShowToast = notification.ShowToast,
                    PlaySound = notification.PlaySound,
                    SoundFile = notification.SoundFile,
                    Tags = new List<string>(notification.Tags),
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = notification.ExpiresAt,
                    IsPersistent = notification.IsPersistent
                };

                var success = await SendToUserAsync(userNotification, cancellationToken);
                if (success)
                {
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
            }
        }

        _logger.LogInformation("Sent notification to {SuccessCount} out of {TotalCount} users", 
            successCount, userIds.Count);

        return successCount;
    }

    public async Task<bool> SendToGroupAsync(string groupName, InAppNotification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(groupName))
            {
                _logger.LogWarning("Cannot send notification - groupName is empty");
                return false;
            }

            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNotification", new
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                Priority = notification.Priority,
                Data = notification.Data,
                ActionUrl = notification.ActionUrl,
                ActionText = notification.ActionText,
                Icon = notification.Icon,
                Avatar = notification.Avatar,
                Sender = notification.Sender,
                GroupId = notification.GroupId,
                ShowToast = notification.ShowToast,
                PlaySound = notification.PlaySound,
                SoundFile = notification.SoundFile,
                Tags = notification.Tags,
                CreatedAt = notification.CreatedAt,
                ExpiresAt = notification.ExpiresAt
            }, cancellationToken);

            _logger.LogInformation("Notification {NotificationId} sent to group {GroupName} via SignalR", 
                notification.Id, groupName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification {NotificationId} to group {GroupName}", 
                notification.Id, groupName);
            return false;
        }
    }

    public async Task<bool> SendToAllAsync(InAppNotification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                Priority = notification.Priority,
                Data = notification.Data,
                ActionUrl = notification.ActionUrl,
                ActionText = notification.ActionText,
                Icon = notification.Icon,
                Avatar = notification.Avatar,
                Sender = notification.Sender,
                GroupId = notification.GroupId,
                ShowToast = notification.ShowToast,
                PlaySound = notification.PlaySound,
                SoundFile = notification.SoundFile,
                Tags = notification.Tags,
                CreatedAt = notification.CreatedAt,
                ExpiresAt = notification.ExpiresAt
            }, cancellationToken);

            _logger.LogInformation("Notification {NotificationId} sent to all connected users via SignalR", 
                notification.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification {NotificationId} to all users", notification.Id);
            return false;
        }
    }

    public async Task<bool> AddUserToGroupAsync(string userId, string groupName, CancellationToken cancellationToken = default)
    {
        try
        {
            var connections = await _connectionManager.GetUserConnectionsAsync(userId);
            
            foreach (var connectionId in connections)
            {
                await _hubContext.Groups.AddToGroupAsync(connectionId, groupName);
            }

            await _connectionManager.AddUserToGroupAsync(userId, groupName);

            _logger.LogInformation("User {UserId} added to group {GroupName}", userId, groupName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user {UserId} to group {GroupName}", userId, groupName);
            return false;
        }
    }

    public async Task<bool> RemoveUserFromGroupAsync(string userId, string groupName, CancellationToken cancellationToken = default)
    {
        try
        {
            var connections = await _connectionManager.GetUserConnectionsAsync(userId);
            
            foreach (var connectionId in connections)
            {
                await _hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName);
            }

            await _connectionManager.RemoveUserFromGroupAsync(userId, groupName);

            _logger.LogInformation("User {UserId} removed from group {GroupName}", userId, groupName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {UserId} from group {GroupName}", userId, groupName);
            return false;
        }
    }

    public async Task<List<string>> GetConnectedUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _connectionManager.GetConnectedUsersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connected users");
            return new List<string>();
        }
    }

    public async Task<bool> IsUserOnlineAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _connectionManager.IsUserOnlineAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserId} is online", userId);
            return false;
        }
    }

    public async Task<List<string>> GetUserConnectionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _connectionManager.GetUserConnectionsAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connections for user {UserId}", userId);
            return new List<string>();
        }
    }
}
