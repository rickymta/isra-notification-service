using Microsoft.AspNetCore.Mvc;
using NotificationService.Api.Models;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace NotificationService.Api.Controllers;

/// <summary>
/// Controller for managing notifications
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly IMessagePublisher _messagePublisher;
    private readonly INotificationHistoryRepository _historyRepository;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        IMessagePublisher messagePublisher,
        INotificationHistoryRepository historyRepository,
        ILogger<NotificationsController> logger)
    {
        _messagePublisher = messagePublisher;
        _historyRepository = historyRepository;
        _logger = logger;
    }

    /// <summary>
    /// Send a notification
    /// </summary>
    /// <param name="request">Notification request details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Notification response with tracking ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SendNotificationResponse), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> SendNotification(
        [FromBody] SendNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Received notification request for channel {Channel}", request.Channel);

            // Validate the request
            var validationResult = ValidateRequest(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.ErrorMessage);
            }

            // Create notification request entity
            var notificationRequest = MapToNotificationRequest(request);

            // Publish to queue
            if (request.ScheduledAt.HasValue && request.ScheduledAt > DateTime.UtcNow)
            {
                var delay = request.ScheduledAt.Value - DateTime.UtcNow;
                await _messagePublisher.PublishNotificationWithDelayAsync(notificationRequest, delay, cancellationToken);
            }
            else
            {
                await _messagePublisher.PublishNotificationAsync(notificationRequest, cancellationToken);
            }

            var response = new SendNotificationResponse
            {
                NotificationId = notificationRequest.Id,
                IsAccepted = true,
                Message = "Notification queued for processing",
                EstimatedProcessingTime = request.ScheduledAt ?? DateTime.UtcNow.AddSeconds(30)
            };

            _logger.LogInformation("Notification {NotificationId} queued successfully", notificationRequest.Id);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification request");
            return StatusCode(500, "Internal server error occurred while processing the notification");
        }
    }

    /// <summary>
    /// Get notification status by ID
    /// </summary>
    /// <param name="id">Notification ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Notification status information</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(NotificationStatusResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetNotificationStatus(
        [FromRoute] string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Notification ID is required");
            }

            var history = await _historyRepository.GetByIdAsync(id, cancellationToken);
            
            if (history == null)
            {
                return NotFound($"Notification with ID {id} not found");
            }

            var response = MapToStatusResponse(history);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification status for ID {NotificationId}", id);
            return StatusCode(500, "Internal server error occurred while retrieving notification status");
        }
    }

    /// <summary>
    /// Get notification history for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user's notification history</returns>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(IEnumerable<NotificationStatusResponse>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetUserNotificationHistory(
        [FromRoute] string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is required");
            }

            var histories = await _historyRepository.GetByUserIdAsync(userId, cancellationToken);
            
            if (!histories.Any())
            {
                return NotFound($"No notifications found for user {userId}");
            }

            var responses = histories.Select(MapToStatusResponse);
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification history for user {UserId}", userId);
            return StatusCode(500, "Internal server error occurred while retrieving notification history");
        }
    }

    private static ValidationResult ValidateRequest(SendNotificationRequest request)
    {
        if (string.IsNullOrEmpty(request.TemplateId) && string.IsNullOrEmpty(request.TemplateName))
        {
            return ValidationResult.Error("Either TemplateId or TemplateName must be provided");
        }

        if (request.Recipient == null)
        {
            return ValidationResult.Error("Recipient information is required");
        }

        switch (request.Channel)
        {
            case Domain.Enums.NotificationChannel.Email:
                if (string.IsNullOrEmpty(request.Recipient.Email))
                {
                    return ValidationResult.Error("Email address is required for email notifications");
                }
                break;

            case Domain.Enums.NotificationChannel.Sms:
                if (string.IsNullOrEmpty(request.Recipient.PhoneNumber))
                {
                    return ValidationResult.Error("Phone number is required for SMS notifications");
                }
                break;

            case Domain.Enums.NotificationChannel.Push:
                if (string.IsNullOrEmpty(request.Recipient.DeviceToken))
                {
                    return ValidationResult.Error("Device token is required for push notifications");
                }
                break;
        }

        return ValidationResult.Valid();
    }

    private static NotificationRequest MapToNotificationRequest(SendNotificationRequest request)
    {
        return new NotificationRequest
        {
            Id = Guid.NewGuid().ToString(),
            TemplateId = request.TemplateId ?? string.Empty,
            TemplateName = request.TemplateName,
            Channel = request.Channel,
            Recipient = new NotificationRecipient
            {
                UserId = request.Recipient.UserId,
                Email = request.Recipient.Email,
                PhoneNumber = request.Recipient.PhoneNumber,
                DeviceToken = request.Recipient.DeviceToken,
                Language = request.Recipient.Language,
                TimeZone = request.Recipient.TimeZone
            },
            Variables = request.Variables,
            ScheduledAt = request.ScheduledAt,
            Priority = request.Priority,
            Metadata = request.Metadata,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static NotificationStatusResponse MapToStatusResponse(NotificationHistory history)
    {
        return new NotificationStatusResponse
        {
            Id = history.Id,
            TemplateName = history.TemplateName,
            Channel = history.Channel,
            Status = history.Status,
            Recipient = new NotificationRecipientDto
            {
                UserId = history.Recipient.UserId,
                Email = history.Recipient.Email,
                PhoneNumber = history.Recipient.PhoneNumber,
                DeviceToken = history.Recipient.DeviceToken,
                Language = history.Recipient.Language,
                TimeZone = history.Recipient.TimeZone
            },
            CreatedAt = history.CreatedAt,
            SentAt = history.SentAt,
            RetryCount = history.RetryCount,
            ErrorMessage = history.ErrorMessage,
            ExternalMessageId = history.ExternalMessageId,
            Metadata = history.Metadata
        };
    }

    private class ValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }

        public static ValidationResult Valid() => new() { IsValid = true };
        public static ValidationResult Error(string message) => new() { IsValid = false, ErrorMessage = message };
    }
}