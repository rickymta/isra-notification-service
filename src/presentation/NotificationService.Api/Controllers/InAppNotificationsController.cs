using Microsoft.AspNetCore.Mvc;
using NotificationService.Api.Models;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace NotificationService.Api.Controllers;

/// <summary>
/// Controller for managing in-app notifications via SignalR
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InAppNotificationsController : ControllerBase
{
    private readonly IInAppNotificationService _inAppNotificationService;
    private readonly IRealtimeNotificationService _realtimeNotificationService;
    private readonly ILogger<InAppNotificationsController> _logger;

    public InAppNotificationsController(
        IInAppNotificationService inAppNotificationService,
        IRealtimeNotificationService realtimeNotificationService,
        ILogger<InAppNotificationsController> logger)
    {
        _inAppNotificationService = inAppNotificationService;
        _realtimeNotificationService = realtimeNotificationService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new in-app notification
    /// </summary>
    /// <param name="request">Notification creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created notification details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(InAppNotificationResponse), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CreateNotification(
        [FromBody] CreateInAppNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating in-app notification for user {UserId}", request.UserId);

            var notification = await _inAppNotificationService.CreateNotificationAsync(
                request.UserId,
                request.Title,
                request.Message,
                NotificationChannel.InApp,
                request.Data,
                request.ExpiresAt,
                cancellationToken);

            var response = MapToResponse(notification);
            return CreatedAtAction(nameof(GetNotificationById), new { id = notification.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating in-app notification for user {UserId}", request.UserId);
            return StatusCode(500, "Internal server error occurred while creating notification");
        }
    }

    /// <summary>
    /// Create multiple in-app notifications in bulk
    /// </summary>
    /// <param name="requests">List of notification creation requests</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created notifications details</returns>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(BulkInAppNotificationResponse), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CreateBulkNotifications(
        [FromBody] BulkCreateInAppNotificationRequest requests,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating {Count} in-app notifications in bulk", requests.Notifications.Count);

            var created = new List<InAppNotification>();
            foreach (var request in requests.Notifications)
            {
                var notification = await _inAppNotificationService.CreateNotificationAsync(
                    request.UserId,
                    request.Title,
                    request.Message,
                    NotificationChannel.InApp,
                    request.Data,
                    request.ExpiresAt,
                    cancellationToken);
                created.Add(notification);
            }

            var response = new BulkInAppNotificationResponse
            {
                TotalCreated = created.Count,
                Notifications = created.Select(MapToResponse).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bulk in-app notifications");
            return StatusCode(500, "Internal server error occurred while creating bulk notifications");
        }
    }

    /// <summary>
    /// Get notifications for a specific user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="isRead">Filter by read status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User's notifications</returns>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(UserNotificationsResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetUserNotifications(
        [FromRoute] string userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isRead = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting notifications for user {UserId}, page: {Page}", userId, page);

            var notifications = await _inAppNotificationService.GetUserNotificationsAsync(
                userId, page, pageSize, isRead, cancellationToken);

            var unreadCount = await _inAppNotificationService.GetUnreadCountAsync(userId, cancellationToken);

            var response = new UserNotificationsResponse
            {
                UserId = userId,
                Notifications = notifications.Select(MapToResponse).ToList(),
                UnreadCount = unreadCount,
                TotalCount = notifications.Count()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
            return StatusCode(500, "Internal server error occurred while retrieving notifications");
        }
    }

    /// <summary>
    /// Get a specific notification by ID
    /// </summary>
    /// <param name="id">Notification identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Notification details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(InAppNotificationResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetNotificationById(
        [FromRoute] string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _inAppNotificationService.GetNotificationAsync(id, cancellationToken);
            
            if (notification == null)
            {
                return NotFound($"Notification with ID {id} not found");
            }

            var response = MapToResponse(notification);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification {NotificationId}", id);
            return StatusCode(500, "Internal server error occurred while retrieving notification");
        }
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    /// <param name="id">Notification identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPut("{id}/read")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> MarkAsRead(
        [FromRoute] string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _inAppNotificationService.MarkAsReadAsync(id, cancellationToken);
            
            if (!success)
            {
                return NotFound($"Notification with ID {id} not found");
            }

            return Ok(new { Message = "Notification marked as read successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
            return StatusCode(500, "Internal server error occurred while updating notification");
        }
    }

    /// <summary>
    /// Mark all notifications as read for a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPut("user/{userId}/read-all")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> MarkAllAsRead(
        [FromRoute] string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _inAppNotificationService.MarkAllAsReadAsync(userId, cancellationToken);

            return Ok(new { Message = "All notifications marked as read successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            return StatusCode(500, "Internal server error occurred while updating notifications");
        }
    }

    /// <summary>
    /// Delete a notification
    /// </summary>
    /// <param name="id">Notification identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> DeleteNotification(
        [FromRoute] string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _inAppNotificationService.DeleteNotificationAsync(id, cancellationToken);
            
            if (!success)
            {
                return NotFound($"Notification with ID {id} not found");
            }

            return Ok(new { Message = "Notification deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId}", id);
            return StatusCode(500, "Internal server error occurred while deleting notification");
        }
    }

    /// <summary>
    /// Delete all notifications for a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpDelete("user/{userId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> DeleteAllUserNotifications(
        [FromRoute] string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _inAppNotificationService.DeleteAllUserNotificationsAsync(userId, cancellationToken);
            return Ok(new { Message = "All user notifications deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all notifications for user {UserId}", userId);
            return StatusCode(500, "Internal server error occurred while deleting notifications");
        }
    }

    /// <summary>
    /// Send a real-time notification to a specific user
    /// </summary>
    /// <param name="request">Real-time notification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPost("realtime/user")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> SendRealtimeToUser(
        [FromBody] SendRealtimeNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _inAppNotificationService.SendRealtimeNotificationAsync(
                request.UserId, "Real-time Notification", request.Message, request.Data, cancellationToken);
            return Ok(new { Message = "Real-time notification sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending real-time notification to user {UserId}", request.UserId);
            return StatusCode(500, "Internal server error occurred while sending notification");
        }
    }

    /// <summary>
    /// Broadcast a real-time notification to all connected users
    /// </summary>
    /// <param name="request">Broadcast notification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPost("realtime/broadcast")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> BroadcastRealtime(
        [FromBody] BroadcastRealtimeNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _inAppNotificationService.BroadcastNotificationAsync(
                "Broadcast Notification", request.Message, request.Data, cancellationToken);
            return Ok(new { Message = "Real-time notification broadcasted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting real-time notification");
            return StatusCode(500, "Internal server error occurred while broadcasting notification");
        }
    }

    private static InAppNotificationResponse MapToResponse(InAppNotification notification)
    {
        return new InAppNotificationResponse
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            Priority = notification.Priority.ToString().ToLower(),
            Data = notification.Data ?? new Dictionary<string, object>(),
            ActionUrl = notification.ActionUrl,
            IsRead = notification.IsRead,
            IsDelivered = notification.IsDelivered,
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt,
            DeliveredAt = notification.DeliveredAt,
            ExpiresAt = notification.ExpiresAt,
            Tags = notification.Tags ?? new List<string>()
        };
    }
}
